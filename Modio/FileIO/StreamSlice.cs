using System;
using System.IO;

namespace Modio.FileIO
{
    public class StreamSlice : Stream
    {
        readonly Stream _parentStream;
        readonly long _sliceOffset;
        readonly int _length;

        public StreamSlice(Stream parentStream, long sliceOffset, int length)
        {
            _parentStream = parentStream;
            _sliceOffset = sliceOffset;
            _length = length;
        }
        
        public override void Flush()
            => throw new NotSupportedException($"{nameof(Flush)} is an invalid method on {nameof(StreamSlice)}");
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _parentStream.Read(buffer, offset, (int)Math.Min(count, Length - Position));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return _parentStream.Seek(_sliceOffset + offset, SeekOrigin.Begin);

                case SeekOrigin.Current: 
                    return _parentStream.Seek(offset, origin);

                case SeekOrigin.End: 
                    return _parentStream.Seek(_sliceOffset + _length + offset, SeekOrigin.Begin);

                default: throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }
        }

        public override void SetLength(long value) 
            => throw new NotSupportedException($"{nameof(SetLength)} is an invalid method on {nameof(StreamSlice)}");
        

        public override void Write(byte[] buffer, int offset, int count)
            => _parentStream.Write(buffer, (int)_sliceOffset + offset, count);

        public override bool CanRead => _parentStream.CanRead;
        public override bool CanSeek => _parentStream.CanSeek;
        public override bool CanWrite => _parentStream.CanWrite;
        public override long Length => _length;
        public override long Position
        {
            get => _parentStream.Position - _sliceOffset;
            set => _parentStream.Position = _sliceOffset + value;
        }
    }
}
