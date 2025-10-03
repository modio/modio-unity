using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Modio.Unity
{
    internal class StreamingDownloadHandler : DownloadHandlerScript
    {
        readonly ChunkedStreamBuffer _streamBuffer;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly TaskCompletionSource<bool> _hasReceivedHeaders =  new TaskCompletionSource<bool>();

        UnityWebRequest _callingRequest;

        /// <summary>
        /// Creates a new DownloadHandlerToStream with a new buffer size of 1MB.
        /// </summary>
        /// <param name="bufferSize"> The size of the buffer to use for receiving data. Default is 1MB.</param>
        /// <param name="token"> Cancellation token to cancel the download operation.</param>
        internal StreamingDownloadHandler(
            int bufferSize = 1024 * 1024,
            CancellationToken token = default
        ) : this(new byte[bufferSize], token) { }

        /// <summary>
        /// Creates a new DownloadHandlerToStream with a specified buffer.
        /// </summary>
        /// <param name="buffer"> The buffer to use for receiving data.</param>
        /// <param name="token"> Cancellation token to cancel the download operation.</param>
        StreamingDownloadHandler(byte[] buffer, CancellationToken token = default) : base(buffer)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _streamBuffer = new ChunkedStreamBuffer(_cancellationTokenSource.Token);
        }

        public void SetCallingRequest(UnityWebRequest request)
        {
            _callingRequest = request;
        }
        /// <summary>
        /// The stream that will receive the downloaded data.
        /// </summary>
        /// <returns> The stream that will receive the downloaded data.</returns>
        internal Stream GetStream() => _streamBuffer;

        protected override bool ReceiveData(byte[] dataReceived, int dataLength)
        {
            if(_cancellationTokenSource.Token.IsCancellationRequested){
                _callingRequest.Abort();
                _streamBuffer.Flush();
                _hasReceivedHeaders.TrySetCanceled();
                //returning false, logs Curl error 23 
                return true;
            }
            
            _streamBuffer.Write(dataReceived, 0, dataLength);
            _hasReceivedHeaders.TrySetResult(true);
            return true;
        }

        /// <summary>
        /// Asynchronously waits until the response headers have been received.
        /// </summary>
        /// <param name="token"> Cancellation token to cancel the wait operation.</param>
        public async Task ResponseReceived(CancellationToken token)
        {
            await _hasReceivedHeaders.Task;
        }

        /// <summary>
        /// Completes the content of the DownloadHandler.
        /// </summary>
        protected override void CompleteContent()
        {
            _hasReceivedHeaders.TrySetResult(true);
            base.CompleteContent();
            _streamBuffer.Complete();
        }

        public async Task WaitForComplete()
        {
            while (!_callingRequest.isDone)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _callingRequest.Abort();
                    _streamBuffer.Flush();
                    _hasReceivedHeaders.TrySetCanceled(_cancellationTokenSource.Token);
                    return;
                }
                await Task.Yield();
            }

            _streamBuffer.Complete();
        }

        /// <summary>
        /// Event called when the download is completed.
        /// </summary>
        /// <param name="_">The AsyncOperation that completed the download.</param>
        public void DownloadCompleted(AsyncOperation _)
        {
            
            if (_callingRequest.result == UnityWebRequest.Result.Success)
                return;

            //Ensure the stream throws an error on read if the request failed
            _streamBuffer.ThrowException = new IOException($"Download failed: {_callingRequest.error}");
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// A stream that receives data from the DownloadHandler.
        /// Backed by a queue of NativeArrays to handle data chunks.
        /// </summary>
        class ChunkedStreamBuffer : Stream
        {
            readonly ConcurrentQueue<BufferChunk> _dataQueue = new ConcurrentQueue<BufferChunk>();
            readonly CancellationToken _shutdownToken;
            readonly AsyncAutoResetEvent _signal = new AsyncAutoResetEvent();
            
            internal ChunkedStreamBuffer(CancellationToken shutdownToken) => _shutdownToken = shutdownToken;

            protected override void Dispose(bool disposing)
            {
                ModioLog.Verbose?.Log("Disposing ChunkedStreamBuffer and its data chunks.");
                while (_dataQueue.TryDequeue(out BufferChunk data))
                    data.Dispose();

                _dataQueue.Clear();
                _signal.Set();
            }

            /// <summary>
            /// Clears the stream, disposing of all queued data.
            /// </summary>
            public override void Flush()
            {
                // Only clear buffered data, do not dispose the stream itself
                ModioLog.Verbose?.Log("Flushing ChunkedStreamBuffer: clearing data chunks.");
                while (_dataQueue.TryDequeue(out BufferChunk data))
                    data.Dispose();
                _dataQueue.Clear();
                _signal.Set();
            }

            public override int Read(byte[] buffer, int offset, int count)
                => ReadAsync(buffer, offset, count, CancellationToken.None).Result;

            public override async Task<int> ReadAsync(
                byte[] buffer,
                int offset,
                int count,
                CancellationToken cancellationToken
            )
            {
              
                
                if (cancellationToken == CancellationToken.None)
                    cancellationToken = _shutdownToken;

                
                // Create a timeout cancellation token to avoid hanging indefinitely
                TimeSpan timeout = TimeSpan.FromSeconds(10);
                using var timeoutTokenSource = new CancellationTokenSource(timeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                
                
                var totalBytesRead = 0;

                while (totalBytesRead < count)
                {
                    BufferChunk data;

                    // Wait for data to be available in the queue
                    while (!_dataQueue.TryPeek(out data))
                    {
                        
                        if (linkedTokenSource.Token.IsCancellationRequested)
                        {
                            if(ThrowException != null)
                                throw ThrowException;
                            
                            if(timeoutTokenSource.Token.IsCancellationRequested)
                                throw new TimeoutException("Read operation timed out after 10 seconds of inactivity.");
                            
                            linkedTokenSource.Token.ThrowIfCancellationRequested();
                        }


                        if (IsDone && _dataQueue.IsEmpty)
                            return totalBytesRead;

                        if (totalBytesRead > 0)
                            return totalBytesRead;
                        
                        await _signal.WaitAsync(linkedTokenSource.Token);

                    }

                    int bytesRead = Math.Min(data.RemainingLength, count - totalBytesRead);
                    NativeArray<byte>.Copy(data.Data, data.Offset, buffer, offset + totalBytesRead, bytesRead);
                    totalBytesRead += bytesRead;

                    data.Offset += bytesRead;

                    if (data.HasData)
                        continue;

                    _dataQueue.TryDequeue(out _);
                    data.Dispose();
                }

                return totalBytesRead;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            /// <inheritdoc />
            public override void Write(byte[] buffer, int offset, int count)
            {
                // If the cancellation token is requested, we stop writing data
                if (_shutdownToken.IsCancellationRequested)
                    return;
                int bytesToWrite = Math.Min(buffer.Length, count);
                var data = new NativeArray<byte>(bytesToWrite, Allocator.Persistent);
                NativeArray<byte>.Copy(buffer, offset, data, 0, bytesToWrite - offset);
                _dataQueue.Enqueue(new BufferChunk(data, 0));
                _signal.Set();

            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => -1; // Length is unknown for this stream

            public override long Position { get; set; } = -1;
            bool IsDone { get; set; }
            public IOException ThrowException { get; set; }

            class BufferChunk : IDisposable
            {
                internal NativeArray<byte> Data { get; }
                internal int Offset { get; set; }
                internal int Length => Data.Length;

                internal bool HasData => Offset < Data.Length;

                internal int RemainingLength => Data.Length - Offset;

                internal BufferChunk(NativeArray<byte> data, int offset)
                {
                    Data = data;
                    Offset = offset;
                }

                
                public void Dispose() => Data.Dispose();
            }

            public void Complete()
            {
                IsDone = true;
                _signal.Set();
            }

        }
        
    }
}
