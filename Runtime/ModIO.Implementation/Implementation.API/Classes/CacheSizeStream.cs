using System;

namespace ModIO.Implementation.API
{
    /// <summary>
    /// This class acts as a stream that only records the size of the stream. None of the
    /// buffer/bytes are copied into the instance, so it doesn't take up memory.
    /// </summary>
    public class CacheSizeStream : System.IO.Stream
    {
        private int totalSize;

        public override void Write(byte[] buffer, int offset, int count)
        {
            totalSize += count;
        }

        public override bool CanRead
        {
            get {
                return false;
            }
        }

        public override bool CanSeek
        {
            get {
                return false;
            }
        }

        public override bool CanWrite
        {
            get {
                return true;
            }
        }

        public override void Flush()
        {
            // Nothing to do
        }

        public override long Length
        {
            get {
                return totalSize;
            }
        }

        public override long Position
        {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
