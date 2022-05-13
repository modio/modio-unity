// TODO(@jackson): Remove
/*
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ModIO.Implementation.Platform;

namespace ModIO.Implementation
{
    /// <summary>
    /// Implementation of a file stream that throws no exceptions and works cross-platform.
    /// </summary>
    /// <remarks>
    /// From: https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-5.0
    /// -- Notes to Implementers --
    /// When you implement a derived class of Stream, you must provide implementations for the
    /// Read(Byte[], Int32, Int32) and Write(Byte[], Int32, Int32) methods. The asynchronous methods
    /// ReadAsync(Byte[], Int32, Int32), WriteAsync(Byte[], Int32, Int32), and CopyToAsync(Stream)
    /// use the synchronous methods Read(Byte[], Int32, Int32) and Write(Byte[], Int32, Int32) in
    /// their implementations. Therefore, your implementations of Read(Byte[], Int32, Int32) and
    /// Write(Byte[], Int32, Int32) will work correctly with the asynchronous methods. The default
    /// implementations of ReadByte() and WriteByte(Byte) create a new single-element byte array,
    /// and then call your implementations of Read(Byte[], Int32, Int32) and Write(Byte[], Int32,
    /// Int32). When you derive from Stream, we recommend that you override these methods to access
    /// your internal buffer, if you have one, for substantially better performance. You must also
    /// provide implementations of CanRead, CanSeek, CanWrite, Flush(), Length, Position,
    /// Seek(Int64, SeekOrigin), and SetLength(Int64).
    ///
    /// Do not override the Close() method, instead, put all the Stream cleanup logic in the
    /// Dispose(Boolean) method. For more information, see Implementing a Dispose Method.
    /// </remarks>
    internal class FileStreamXXX : System.IO.Stream
    {
        /// <summary>Data service used to access the file.</summary>
        IDataService dataService = null;

        /// <summary>File path of the file being accessed.</summary>
        string filePath = null;

        /// <summary>The result of the last operation attempted on the FileStream.</summary>
        Result result = ResultBuilder.Success;

        /// <summary>Is the throwing of exceptions permitted?</summary>
        bool areExceptionsEnabled = true;

#region Properties

        /// <summary>A value indicating whether the current stream supports reading.</summary>
        public override bool CanRead => throw new NotImplementedException();

        /// <summary>A value indicating whether the current stream supports seeking.</summary>
        public override bool CanSeek => throw new NotImplementedException();

        /// <summary>A value that determines whether the current stream can time out.</summary>
        /// <remarks>
        /// The CanTimeout property always returns false. Some stream implementations require
        /// different behavior, such as NetworkStream, which times out if network connectivity is
        /// interrupted or lost. If you are implementing a stream that must be able to time out,
        /// this property should be overridden to return true.
        /// </remarks>
        public override bool CanTimeout => base.CanTimeout;

        /// <summary>A value indicating whether the current stream supports writing.</summary>
        public override bool CanWrite => throw new NotImplementedException();

        /// <summary>The length in bytes of the stream.</summary>
        public override long Length => throw new NotImplementedException();

        /// <summary>Gets or sets the position within the current stream.</summary>
        /// <remarks>
        /// The stream must support seeking to get or set the position. Use the CanSeek property to
        /// determine whether the stream supports seeking.
        ///
        /// Seeking to any location beyond the length of the stream is supported.
        ///
        /// The Position property does not keep track of the number of bytes from the stream that
        /// have been consumed, skipped, or both.
        /// </remarks>
        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets a value, in milliseconds, that determines how long the stream will attempt
        /// to read before timing out.
        /// </summary>
        /// <remarks>
        /// The ReadTimeout property should be overridden to provide the appropriate behavior for
        /// the stream. If the stream does not support timing out, this property should raise an
        /// InvalidOperationException.
        /// </remarks>
        public override int ReadTimeout
        {
            get => base.ReadTimeout;
            set => base.ReadTimeout = value;
        }

        /// <summary>
        /// Gets or sets a value, in milliseconds, that determines how long the stream will attempt
        /// to write before timing out.
        /// </summary>
        /// <remarks>
        /// The WriteTimeout property should be overridden to provide the appropriate behavior for
        /// the stream. If the stream does not support timing out, this property should raise an
        /// InvalidOperationException.
        /// </remarks>
        public override int WriteTimeout
        {
            get => base.WriteTimeout;
            set => base.WriteTimeout = value;
        }

#endregion // Properties

#region Initialization

        /// <summary>
        /// Initializes the FileStream with the given file path using the given service.
        /// </summary>
        public FileStreamXXX(IDataService dataService, string filePath, bool enableExceptions)
        {
            this.dataService = dataService;
            this.filePath = filePath;
            this.areExceptionsEnabled = enableExceptions;
        }

#endregion // Initialization

#region mod.io Functionality

        /// <summary>Gets the result of the last operation attempted on the FileStream.</summary>
        public Result GetLastResult()
        {
            return this.result;
        }

#endregion // mod.io Functionality

#region System.IO.Stream Interface

        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file handles)
        /// associated with the current stream. Instead of calling this method, ensure that the
        /// stream is properly disposed.
        /// </summary>
        public override void Close()
        {
            /// In derived classes, do not override the Close() method, instead, put all of the
            /// Stream cleanup logic in the Dispose(Boolean) method. For more information, see
            /// Implementing a Dispose Method.

            base.Close();
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another
        /// stream.
        /// </summary>
        /// <param name="destination">Stream - The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">Int32 - The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
        /// <param name="cancellationToken">CancellationToken - The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>Task - A task that represents the asynchronous copy operation.</returns>
        /// <remarks>
        /// The CopyToAsync method enables you to perform resource-intensive I/O operations without
        /// blocking the main thread. This performance consideration is particularly important in a
        /// Windows 8.x Store app or desktop app where a time-consuming stream operation can block
        /// the UI thread and make your app appear as if it is not working. The async methods are
        /// used in conjunction with the async and await keywords in Visual Basic and C#.
        ///
        /// If the operation is canceled before it completes, the returned task contains the
        /// Canceled value for the Status property.
        ///
        /// Copying begins at the current position in the current stream.
        ///
        /// For an example of copying between two streams, see the CopyToAsync(Stream) overload.
        /// </remarks>
        public override Task CopyToAsync(Stream destination, int bufferSize,
                                         CancellationToken cancellationToken)
        {
            // Exceptions
            // ArgumentNullException
            //  destination is null.
            // ArgumentOutOfRangeException
            //  buffersize is negative or zero.
            // ObjectDisposedException
            //  Either the current stream or the destination stream is disposed.
            // NotSupportedException
            //  The current stream does not support reading, or the destination stream does not
            //  support writing.

            return base.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any
        /// buffered data to be written to the underlying device.
        /// </summary>
        /// <remarks>
        /// Override Flush on streams that implement a buffer. Use this method to move any
        /// information from an underlying buffer to its destination, clear the buffer, or both.
        /// Depending upon the state of the object, you might have to modify the current position
        /// within the stream (for example, if the underlying stream supports seeking). For
        /// additional information see CanSeek.
        ///
        /// In a class derived from Stream that doesn't support writing, Flush is typically
        /// implemented as an empty method to ensure full compatibility with other Stream types
        /// since it's valid to flush a read-only stream.
        ///
        /// When using the StreamWriter or BinaryWriter class, do not flush the base Stream object.
        /// Instead, use the class's Flush or Close method, which makes sure that the data is
        /// flushed to the underlying stream first and then written to the file.
        /// </remarks>
        public override void Flush()
        {
            // Exceptions
            // IOException
            //  An I/O error occurs.

            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously clears all buffers for this stream and causes any buffered data to be
        /// written to the underlying device.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken - The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>Task - A task that represents the asynchronous flush operation.</returns>
        /// <remarks>
        /// If the operation is canceled before it completes, the returned task contains the
        /// Canceled value for the Status property.
        ///
        /// If a derived class, such as DeflateStream or GZipStream, does not flush the buffer in
        /// its implementation of the Flush method, the FlushAsync method will not flush the buffer.
        /// </remarks>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // Exceptions
            // ObjectDisposedException
            //  The stream has been disposed.
            return base.FlushAsync(cancellationToken);
        }


        public override object InitializeLifetimeService()
        {
            return base.InitializeLifetimeService();
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream
        /// and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">
        /// Byte[] - An array of bytes. When this method returns, the buffer contains the specified
        /// byte array with the values between offset and (offset + count - 1) replaced by the bytes
        /// read from the current source.
        /// </param>
        /// <param name="offset">Int32 - The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">Int32 - The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// Int32 - The total number of bytes read into the buffer. This can be less than the number
        /// of bytes requested if that many bytes are not currently available, or zero (0) if the
        /// end of the stream has been reached.
        /// </returns>
        /// <remarks>
        /// Use the CanRead property to determine whether the current instance supports reading. Use
        /// the ReadAsync method to read asynchronously from the current stream.
        ///
        /// Implementations of this method read a maximum of count bytes from the current stream and
        /// store them in buffer beginning at offset. The current position within the stream is
        /// advanced by the number of bytes read; however, if an exception occurs, the current
        /// position within the stream remains unchanged. Implementations return the number of bytes
        /// read. The implementation will block until at least one byte of data can be read, in the
        /// event that no data is available. Read returns 0 only when there is no more data in the
        /// stream and no more is expected (such as a closed socket or end of file). An
        /// implementation is free to return fewer bytes than requested even if the end of the
        /// stream has not been reached.
        ///
        /// Use BinaryReader for reading primitive data types.
        /// </remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Exceptions
            // ArgumentException
            //  The sum of offset and count is larger than the buffer length.
            // ArgumentNullException
            //  buffer is null.
            // ArgumentOutOfRangeException
            //  offset or count is negative.
            // IOException
            //  An I/O error occurs.
            // NotSupportedException
            //  The stream does not support reading.
            // ObjectDisposedException
            //  Methods were called after the stream was closed.

            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream and advances the
        /// position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">Byte[] - The buffer to write the data into.</param>
        /// <param name="offset">Int32 - The byte offset in buffer at which to begin writing data from the stream.</param>
        /// <param name="count">Int32 - The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">CancellationToken - The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>
        /// Task<Int32> - A task that represents the asynchronous read operation. The value of the
        /// TResult parameter contains the total number of bytes read into the buffer. The result
        /// value can be less than the number of bytes requested if the number of bytes currently
        /// available is less than the requested number, or it can be 0 (zero) if the end of the
        /// stream has been reached.
        /// </returns>
        /// <remarks>
        /// The ReadAsync method enables you to perform resource-intensive I/O operations without
        /// blocking the main thread. This performance consideration is particularly important in a
        /// Windows 8.x Store app or desktop app where a time-consuming stream operation can block
        /// the UI thread and make your app appear as if it is not working. The async methods are
        /// used in conjunction with the async and await keywords in Visual Basic and C#.
        ///
        /// Use the CanRead property to determine whether the current instance supports reading.
        ///
        /// If the operation is canceled before it completes, the returned task contains the
        /// Canceled value for the Status property.
        ///
        /// For an example, see the ReadAsync(Byte[], Int32, Int32) overload.
        /// </remarks>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
                                            CancellationToken cancellationToken)
        {
            // Exceptions
            // ArgumentNullException
            //  buffer is null.
            // ArgumentOutOfRangeException
            //  offset or count is negative.
            // ArgumentException
            //  The sum of offset and count is larger than the buffer length.
            // NotSupportedException
            //  The stream does not support reading.
            // ObjectDisposedException
            //  The stream has been disposed.
            // InvalidOperationException
            //  The stream is currently in use by a previous read operation.

            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or
        /// returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>Int32 - The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        /// <remarks>
        /// Use the CanRead property to determine whether the current instance supports reading.
        ///
        /// Attempts to manipulate the stream after the stream has been closed could throw an
        /// ObjectDisposedException.
        ///
        /// - Notes to Inheritors -
        /// The default implementation on Stream creates a new single-byte array and then calls
        /// Read(Byte[], Int32, Int32). While this is formally correct, it is inefficient. Any
        /// stream with an internal buffer should override this method and provide a much more
        /// efficient version that reads the buffer directly, avoiding the extra array allocation on
        /// every call.
        /// </remarks>
        public override int ReadByte()
        {
            // Exceptions
            // NotSupportedException
            //  The stream does not support reading.
            // ObjectDisposedException
            //  Methods were called after the stream was closed.

            return base.ReadByte();
        }

        /// <summary>When overridden in a derived class, sets the position within the current
        /// stream.</summary>
        /// <param name="offset">Int64 - A byte offset relative to the origin parameter.</param>
        /// <param name="origin">SeekOrigin - A value of type SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>Int64 - The new position within the current stream.</returns>
        /// <remarks>
        /// Use the CanSeek property to determine whether the current instance supports seeking.
        ///
        /// If offset is negative, the new position is required to precede the position specified by
        /// origin by the number of bytes specified by offset. If offset is zero (0), the new
        /// position is required to be the position specified by origin. If offset is positive, the
        /// new position is required to follow the position specified by origin by the number of
        /// bytes specified by offset.
        ///
        /// Classes derived from Stream that support seeking must override this method to provide
        /// the functionality described above.
        ///
        /// Seeking to any location beyond the length of the stream is supported.
        /// </remarks>
        public override long Seek(long offset, SeekOrigin origin)
        {
            // Exceptions
            // IOException
            //  An I/O error occurs.
            // NotSupportedException
            //  The stream does not support seeking, such as if the stream is constructed from a
            //  pipe or console output.
            // ObjectDisposedException
            //  Methods were called after the stream was closed.

            throw new NotImplementedException();
        }

        /// <summary>When overridden in a derived class, sets the length of the current
        /// stream.</summary>
        /// <param name="value">Int64 - The desired length of the current stream in bytes.</param>
        /// <remarks>
        /// If the specified value is less than the current length of the stream, the stream is
        /// truncated. If the specified value is larger than the current length of the stream, the
        /// stream is expanded. If the stream is expanded, the contents of the stream between the
        /// old and the new length are not defined.
        ///
        /// A stream must support both writing and seeking for SetLength to work.
        ///
        /// Use the CanWrite property to determine whether the current instance supports writing,
        /// and the CanSeek property to determine whether seeking is supported.
        /// </remarks>
        public override void SetLength(long value)
        {
            // Exceptions
            // IOException
            //  An I/O error occurs.
            // NotSupportedException
            //  The stream does not support both writing and seeking, such as if the stream is
            //  constructed from a pipe or console output.
            // ObjectDisposedException
            //  Methods were called after the stream was closed.

            throw new NotImplementedException();
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and
        /// advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">Byte[]- An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">Int32- The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">Int32- The number of bytes to be written to the current stream.</param>
        /// <remarks>
        /// Use the CanWrite property to determine whether the current instance supports writing.
        /// Use the WriteAsync method to write asynchronously to the current stream.
        ///
        /// If the write operation is successful, the position within the stream advances by the
        /// number of bytes written. If an exception occurs, the position within the stream remains
        /// unchanged.
        /// </remarks>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Exceptions
            // ArgumentException
            //  The sum of offset and count is greater than the buffer length.
            // ArgumentNullException
            //  buffer is null.
            // ArgumentOutOfRangeException
            //  offset or count is negative.
            // IOException
            //  An I/O error occurred, such as the specified file cannot be found.
            // NotSupportedException
            //  The stream does not support writing.
            // ObjectDisposedException
            //  Write(Byte[], Int32, Int32) was called after the stream was closed.

            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously writes a sequence of bytes to the current stream, advances the current
        /// position within this stream by the number of bytes written, and monitors cancellation
        /// requests.
        /// </summary>
        /// <param name="buffer">Byte[] - The buffer to write data from.</param>
        /// <param name="offset">Int32 - The zero-based byte offset in buffer from which to begin copying bytes to the stream.</param>
        /// <param name="count">Int32 - The maximum number of bytes to write.</param>
        /// <param name="cancellationToken">CancellationToken - The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>Task - A task that represents the asynchronous write operation.</returns>
        /// <remarks>
        /// The WriteAsync method enables you to perform resource-intensive I/O operations without
        /// blocking the main thread. This performance consideration is particularly important in a
        /// Windows 8.x Store app or desktop app where a time-consuming stream operation can block
        /// the UI thread and make your app appear as if it is not working. The async methods are
        /// used in conjunction with the async and await keywords in Visual Basic and C#.
        ///
        /// Use the CanWrite property to determine whether the current instance supports writing.
        ///
        /// If the operation is canceled before it completes, the returned task contains the
        /// Canceled value for the Status property.
        ///
        /// For an example, see the WriteAsync(Byte[], Int32, Int32) overload.
        /// </remarks>
        public override Task WriteAsync(byte[] buffer, int offset, int count,
                                        CancellationToken cancellationToken)
        {
            // Exceptions
            // ArgumentNullException
            //  buffer is null.
            // ArgumentOutOfRangeException
            //  offset or count is negative.
            // ArgumentException
            //  The sum of offset and count is larger than the buffer length.
            // NotSupportedException
            //  The stream does not support writing.
            // ObjectDisposedException
            //  The stream has been disposed.
            // InvalidOperationException
            //  The stream is currently in use by a previous write operation.

            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the
        /// stream by one byte.
        /// </summary>
        /// <param name="value">Byte - The byte to write to the stream.</param>
        /// <remarks>
        /// Use the CanWrite property to determine whether the current instance supports writing.
        ///
        /// - Notes to Inheritors -
        /// The default implementation on Stream creates a new single-byte array and then calls
        /// Write(Byte[], Int32, Int32). While this is formally correct, it is inefficient. Any
        /// stream with an internal buffer should override this method and provide a much more
        /// efficient version that writes to the buffer directly, avoiding the extra array
        /// allocation on every call.
        /// </remarks>
        public override void WriteByte(byte value)
        {
            // Exceptions
            // IOException
            //  An I/O error occurs.
            // NotSupportedException
            //  The stream does not support writing, or the stream is already closed.
            // ObjectDisposedException
            //  Methods were called after the stream was closed.

            base.WriteByte(value);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Stream and optionally releases the managed
        /// resources.
        /// </summary>
        /// <param name="disposing">
        /// Boolean - true to release both managed and unmanaged resources; false to release only
        /// unmanaged resources.
        /// </param>
        /// <remarks>
        /// You should release all resources by specifying true for disposing. When disposing is
        /// true, the stream can also ensure data is flushed to the underlying buffer, and access
        /// other finalizable objects. This may not be possible when called from a finalizer due a
        /// lack of ordering among finalizers.
        ///
        /// If your stream is using an operating system handle to communicate with its source,
        /// consider using a subclass of SafeHandle for this purpose.
        ///
        /// This method is called by the public Dispose method and the Finalize method. Dispose
        /// invokes the protected Dispose method with the disposing parameter set to true. Finalize
        /// invokes Dispose with disposing set to false.
        ///
        /// - Notes to Inheritors -
        ///
        /// In derived classes, do not override the Close() method, instead, put all of the Stream
        /// cleanup logic in the Dispose(Boolean) method.
        ///
        /// Dispose() can be called multiple times by other objects. When overriding
        /// Dispose(Boolean), be careful not to reference objects that have been previously disposed
        /// of in an earlier call to Dispose(). For more information about how to implement
        /// Dispose(Boolean), see Implementing a Dispose Method.
        ///
        /// For more information about Dispose() and Finalize(), see Cleaning Up Unmanaged
        /// Resources.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }


#endregion // System.IO.Stream Interface

#region Object Interface

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

#endregion // Object Interface

#region Obsolete

        /// <summary>Begins an asynchronous read operation.</summary>
        /// <param name="buffer">Byte[] - The buffer to read the data into.</param>
        /// <param name="offset">Int32 - The byte offset in buffer at which to begin writing data read from the stream.</param>
        /// <param name="count">Int32 - The maximum number of bytes to read.</param>
        /// <param name="callback">AsyncCallback - An optional asynchronous callback, to be called when the read is complete.</param>
        /// <param name="state">Object - A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>IAsyncResult - An IAsyncResult that represents the asynchronous read, which could still be pending.</returns>
        [Obsolete("Use ReadAsync instead.")]
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count,
                                               AsyncCallback callback, object state)
        {
            /// In the .NET Framework 4 and earlier versions, you have to use methods such as
            /// BeginRead and EndRead to implement asynchronous I/O operations. These methods are
            /// still available in the .NET Framework 4.5 to support legacy code; however, the new
            /// async methods, such as ReadAsync, WriteAsync, CopyToAsync, and FlushAsync, help you
            /// implement asynchronous I/O operations more easily.
            ///
            /// The default implementation of BeginRead on a stream calls the Read method
            /// synchronously, which means that Read might block on some streams. However, instances
            /// of classes such as FileStream and NetworkStream fully support asynchronous
            /// operations if the instances have been opened asynchronously. Therefore, calls to
            /// BeginRead will not block on those streams. You can override BeginRead (by using
            /// async delegates, for example) to provide asynchronous behavior.
            ///
            /// Pass the IAsyncResult return value to the EndRead method of the stream to determine
            /// how many bytes were read and to release operating system resources used for reading.
            /// EndRead must be called once for every call to BeginRead. You can do this either by
            /// using the same code that called BeginRead or in a callback passed to BeginRead.
            ///
            /// The current position in the stream is updated when the asynchronous read or write is
            /// issued, not when the I/O operation completes.
            ///
            /// Multiple simultaneous asynchronous requests render the request completion order
            /// uncertain.
            ///
            /// Use the CanRead property to determine whether the current instance supports reading.
            ///
            /// If a stream is closed or you pass an invalid argument, exceptions are thrown
            /// immediately from BeginRead. Errors that occur during an asynchronous read request,
            /// such as a disk failure during the I/O request, occur on the thread pool thread and
            /// throw exceptions when calling EndRead.

#if MODIO_COMPLILE_ALL && UNITY_EDITOR
            throw new System.NotImplementedException("This should never be called.");
#endif

            return base.BeginRead(buffer, offset, count, callback, state);
        }

        /// <summary>Begins an asynchronous write operation.</summary>
        /// <param name="buffer">Byte[] - The buffer to write data from.</param>
        /// <param name="offset">Int32 - The byte offset in buffer from which to begin writing.</param>
        /// <param name="count">Int32 - The maximum number of bytes to write.</param>
        /// <param name="callback">AsyncCallback - An optional asynchronous callback, to be called when the write is complete.</param>
        /// <param name="state">Object - A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>IAsyncResult - An IAsyncResult that represents the asynchronous write, which could still be pending.</returns>
        [Obsolete("Use WriteAsync instead.")]
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count,
                                                AsyncCallback callback, object state)
        {
            /// In the .NET Framework 4 and earlier versions, you have to use methods such as
            /// BeginWrite and EndWrite to implement asynchronous I/O operations. These methods are
            /// still available in the .NET Framework 4.5 to support legacy code; however, the new
            /// async methods, such as ReadAsync, WriteAsync, CopyToAsync, and FlushAsync, help you
            /// implement asynchronous I/O operations more easily.
            ///
            /// The default implementation of BeginWrite on a stream calls the Write method
            /// synchronously, which means that Write might block on some streams. However,
            /// instances of classes such as FileStream and NetworkStream fully support asynchronous
            /// operations if the instances have been opened asynchronously. Therefore, calls to
            /// BeginWrite will not block on those streams. You can override BeginWrite (by using
            /// async delegates, for example) to provide asynchronous behavior.
            ///
            /// Pass the IAsyncResult returned by the current method to EndWrite to ensure that the
            /// write completes and frees resources appropriately. EndWrite must be called once for
            /// every call to BeginWrite. You can do this either by using the same code that called
            /// BeginWrite or in a callback passed to BeginWrite. If an error occurs during an
            /// asynchronous write, an exception will not be thrown until EndWrite is called with
            /// the IAsyncResult returned by this method.
            ///
            /// If a stream is writable, writing at the end of the stream expands the stream.
            ///
            /// The current position in the stream is updated when you issue the asynchronous read
            /// or write, not when the I/O operation completes. Multiple simultaneous asynchronous
            /// requests render the request completion order uncertain.
            ///
            /// Use the CanWrite property to determine whether the current instance supports
            /// writing.
            ///
            /// If a stream is closed or you pass an invalid argument, exceptions are thrown
            /// immediately from BeginWrite. Errors that occur during an asynchronous write request,
            /// such as a disk failure during the I/O request, occur on the thread pool thread and
            /// throw exceptions when calling EndWrite.

#if MODIO_COMPLILE_ALL && UNITY_EDITOR
            throw new System.NotImplementedException("This should never be called.");
#endif

            return base.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <summary>Waits for the pending asynchronous read to complete.</summary>
        /// <param name="asyncResult">IAsyncResult - The reference to the pending asynchronous request to finish.</param>
        /// <returns>Int32 - The number of bytes read from the stream, between zero (0) and the
        /// number of bytes you requested. Streams return zero (0) only at the end of the stream,
        /// otherwise, they should block until at least one byte is available.</returns>
        public override int EndRead(IAsyncResult asyncResult)
        {
            /// In the .NET Framework 4 and earlier versions, you have to use methods such as
            /// BeginRead and EndRead to implement asynchronous I/O operations. These methods are
            /// still available in the .NET Framework 4.5 to support legacy code; however, the new
            /// async methods, such as ReadAsync, WriteAsync, CopyToAsync, and FlushAsync, help you
            /// implement asynchronous I/O operations more easily.
            ///
            /// Call EndRead to determine how many bytes were read from the stream.
            ///
            /// EndRead can be called once on every IAsyncResult from BeginRead.
            ///
            /// This method blocks until the I/O operation has completed.

#if MODIO_COMPLILE_ALL && UNITY_EDITOR
            throw new System.NotImplementedException("This should never be called.");
#endif

            return base.EndRead(asyncResult);
        }

        /// <summary>Ends an asynchronous write operation.</summary>
        /// <param name="asyncResult">IAsyncResult - A reference to the outstanding asynchronous I/O request.</param>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            /// In the .NET Framework 4 and earlier versions, you have to use methods such as
            /// BeginWrite and EndWrite to implement asynchronous I/O operations. These methods are
            /// still available in the .NET Framework 4.5 to support legacy code; however, the new
            /// async methods, such as ReadAsync, WriteAsync, CopyToAsync, and FlushAsync, help you
            /// implement asynchronous I/O operations more easily.
            ///
            /// EndWrite must be called exactly once on every IAsyncResult from BeginWrite.
            ///
            /// This method blocks until the I/O operation has completed. Errors that occur during
            /// an asynchronous write request, such as a disk failure during the I/O request, occur
            /// on the thread pool thread and become visible upon a call to EndWrite. Exceptions
            /// thrown by the thread pool thread will not be visible when calling EndWrite.

#if MODIO_COMPLILE_ALL && UNITY_EDITOR
            throw new System.NotImplementedException("This should never be called.");
#endif

            base.EndWrite(asyncResult);
        }

        /// <summary>Allocates a WaitHandle object.</summary>
        [System.Obsolete(
            "CreateWaitHandle will be removed eventually.  Please use \"new
ManualResetEvent(false)\" instead.")] protected override WaitHandle CreateWaitHandle()
        {
            return base.CreateWaitHandle();
        }

#endregion // Obsolete
    }
}
*/
