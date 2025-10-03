using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Checksum;

namespace Modio.FileIO
{
    public class CRCComputingStreamWrapper : Stream
    {
        readonly Stream _baseStream;
        readonly Crc32 _crc;
        bool _hasTransformedFinalBlock;

        readonly bool _isReadOnly;
        readonly bool _isOwner;

        /// <summary>
        /// Creates a new instance of the CRCComputingStreamWrapper that computes CRC32 checksums for read operations.
        /// </summary>
        /// <param name="baseStream">The base stream to wrap.</param>
        /// <param name="streamOwner">Indicates whether the CRCComputingStreamWrapper owns the base stream and should dispose of it when done.</param>
        /// <returns>The CRCComputingStreamWrapper instance.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static CRCComputingStreamWrapper ReadOnly(Stream baseStream, bool streamOwner = false)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream), "Base stream cannot be null.");

            if (baseStream.CanRead == false)
                throw new ArgumentException("Base stream must be readable.", nameof(baseStream));

            return new CRCComputingStreamWrapper(baseStream, true, streamOwner);
        }

        /// <summary>
        /// Creates a new instance of the CRCComputingStreamWrapper that computes CRC32 checksums for write operations.
        /// </summary>
        /// <param name="baseStream">The base stream to wrap.</param>
        /// <param name="streamOwner">Indicates whether the CRCComputingStreamWrapper owns the base stream and should dispose of it when done.</param>
        /// <returns>The CRCComputingStreamWrapper instance.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static CRCComputingStreamWrapper WriteOnly(Stream baseStream, bool streamOwner = false)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream), "Base stream cannot be null.");

            if (baseStream.CanWrite == false)
                throw new ArgumentException("Base stream must be writable.", nameof(baseStream));

            return new CRCComputingStreamWrapper(baseStream, false, streamOwner);
        }

        CRCComputingStreamWrapper(Stream baseStream, bool isReadOnly, bool streamOwner = false)
        {
            _baseStream = baseStream;
            _crc = new Crc32();
            _isReadOnly = isReadOnly;
            _isOwner = streamOwner;
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                if (_isOwner)
                    _baseStream.Dispose();

            base.Dispose(disposing);
        }

        public long GetCrcValue() => _crc.Value;

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_isReadOnly)
                throw new NotSupportedException(
                    "Write operation is not supported in CRCComputingStreamWrapper when created for read-only."
                );

            await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
            var segment = new ArraySegment<byte>(buffer, offset, count);
            _crc.Update(segment);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_isReadOnly)
                throw new NotSupportedException(
                    "Read operation is not supported in CRCComputingStreamWrapper when created for write-only."
                );

            int read = _baseStream.Read(buffer, offset, count);

            if (read == 0)
                return read;

            var segment = new ArraySegment<byte>(buffer, offset, read);
            _crc.Update(segment);

            return read;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        )
        {
            if (!_isReadOnly)
                throw new NotSupportedException(
                    "Read operation is not supported in CRCComputingStreamWrapper when created for write-only."
                );

            int read = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);

            if (read == 0)
                return read;

            var segment = new ArraySegment<byte>(buffer, offset, read);
            _crc.Update(segment);

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException("Seek operation is not supported in CRCComputingStreamWrapper.");

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_isReadOnly)
                throw new NotSupportedException(
                    "Write operation is not supported in CRCComputingStreamWrapper when created for read-only."
                );
            
            _baseStream.Write(buffer, offset, count);
            var segment = new ArraySegment<byte>(buffer, offset, count);
            _crc.Update(segment);
        }

        public override bool CanRead => _isReadOnly && _baseStream.CanRead;
        public override bool CanWrite => !_isReadOnly && _baseStream.CanWrite;

        public override bool CanSeek => false;

        public override long Length => _baseStream.Length;
        public override long Position
        {
            get => _baseStream.Position;
            set => throw new NotSupportedException("Setting position is not supported in CRCComputingStreamWrapper.");
        }
    }
}
