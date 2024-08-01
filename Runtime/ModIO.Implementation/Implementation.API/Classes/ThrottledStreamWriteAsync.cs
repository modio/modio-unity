using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ModIO.Implementation.API
{
    public class ThrottledStreamWriteAsync : Stream
    {
        readonly Stream _stream;
        readonly long _maxBytesPerSecond;
        readonly Stopwatch _stopwatch = new Stopwatch();
        long _bytes;

        public long BytesPerSecond { get; private set; }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public ThrottledStreamWriteAsync(Stream stream, long maxBytesPerSecond)
        {
            _stream = stream;
            _maxBytesPerSecond = maxBytesPerSecond;
        }

        public override void Flush() => _stream.Flush();

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (!_stopwatch.IsRunning || _stopwatch.ElapsedMilliseconds >= 1000)
                {
                    _stopwatch.Restart();
                    BytesPerSecond = _bytes;
                    _bytes = 0;
                }

                if (count <= _maxBytesPerSecond - _bytes)
                {
                    await _stream.WriteAsync(buffer, offset, count, cancellationToken);
                    _bytes += count;

                    return;
                }

                await Task.Delay(1000 - (int)_stopwatch.ElapsedMilliseconds, cancellationToken);
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => _stream.SetLength(value);
        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }
    }
}
