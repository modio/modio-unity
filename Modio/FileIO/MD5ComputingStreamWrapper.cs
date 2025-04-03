using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Modio.FileIO
{
    public class MD5ComputingStreamWrapper : Stream
    {
        public int TotalBytesRead { get; private set; }
        
        Stream _baseStream;
        MD5 _md5;
        bool _hasTransformedFinalBlock;

        public MD5ComputingStreamWrapper(Stream baseStream)
        {
            _baseStream = baseStream;
            _md5 = MD5.Create();
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _md5.Dispose();
                _baseStream.Dispose();
            }
            
            base.Dispose(disposing);
        }

        public async Task<string> GetMD5HashAsync()
        {
            var buffer = new byte[4096];

            while (!_hasTransformedFinalBlock)
            {
                int bytesRead = await ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
            }

            return BitConverter.ToString(_md5.Hash);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);

            if (bytesRead != 0)
                _md5.TransformBlock(buffer, offset, bytesRead, null, 0);
            else if(!_hasTransformedFinalBlock)
            {
                _hasTransformedFinalBlock = true;
                _md5.TransformFinalBlock(buffer, 0, 0);
            }
            
            TotalBytesRead += bytesRead;
            
            return bytesRead;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _baseStream.Read(buffer, offset, count);

            if (bytesRead != 0)
                _md5.TransformBlock(buffer, offset, bytesRead, null, 0);
            else if(!_hasTransformedFinalBlock)
            {
                _hasTransformedFinalBlock = true;
                _md5.TransformFinalBlock(buffer, 0, 0);
            }
            TotalBytesRead += bytesRead;

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _baseStream.Length;
        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }
    }
}
