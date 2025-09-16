using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Modio.FileIO
{
    public class ModioZipInputStream : ZipInputStream
    {
        MemoryStream _centralDirectoryStream;
        readonly byte[] _headerBytes = new byte[4];
        bool _foundEndOfCentralDirectory;
        bool _noMoreLocalEntries;
        
        public ModioZipInputStream(Stream baseInputStream, int bufferSize) : base(baseInputStream, bufferSize)
        {
            
        }
        
        public ModioZipInputStream(Stream baseInputStream) : base(baseInputStream)
        {
            
        }

        void UpdateHeaderBytes(byte newByte)
        {
            _headerBytes[0] = _headerBytes[1];
            _headerBytes[1] = _headerBytes[2];
            _headerBytes[2] = _headerBytes[3];
            _headerBytes[3] = newByte;
        }
        
        public ZipEntry GetNextEntryWithBackTrack()
        {
            if (inputBuffer.RawLength > 0)
                for (int i = Math.Max(0, inputBuffer.RawLength - 4); i < inputBuffer.RawLength; i++)
                    UpdateHeaderBytes(inputBuffer.RawData[i]);

            ZipEntry output = GetNextEntry();

            if (output != null)
                return output;


            int offset = inputBuffer.RawLength - inputBuffer.Available;

            if (offset < 4)
                for (var i = 0; i < offset; i++)
                    UpdateHeaderBytes(inputBuffer.RawData[i]);
            else
                Buffer.BlockCopy(inputBuffer.RawData, offset - 4, _headerBytes, 0, 4);

            int signature = ReadLeInt(_headerBytes, 0);

            _noMoreLocalEntries = true;

            // Check if the signature matches the central directory signature
            // Zip64 uses the same signature
            if (signature is not (ZipConstants.CentralHeaderSignature))
                return null;

            _centralDirectoryStream = new MemoryStream();

            return null;
        }

        void StoreUntilEnd(byte[] buffer, int offset, int count)
        {
            // If we have already found the end of the central directory, we can stop reading
            if (_foundEndOfCentralDirectory)
                return;
                
            // Read until we hit the end of the central directory
            for (int i = offset; i < count; i++)
            {
                byte currentByte = buffer[i];
                byte lastByte = _headerBytes[0];
                UpdateHeaderBytes(currentByte);
                
                int signature = ReadLeInt(_headerBytes, 0);
                
                // If we hit the end of the central directory, we can stop reading
                // Zip64 uses a different signature for the end of the central directory
                if (signature is ZipConstants.EndOfCentralDirectorySignature
                                 or ZipConstants.Zip64CentralFileHeaderSignature)
                {
                    _foundEndOfCentralDirectory = true;
                    _centralDirectoryStream.WriteByte(lastByte);
                    break;
                }
                
                // write the byte we just popped off the array;
                _centralDirectoryStream.WriteByte(lastByte);
            }
        }

        public async Task ReadUntilEndAsync(CancellationToken token)
        {
            var buffer = new byte[4096];
            while (true)
            {
                int readSize = await ReadAsync(buffer, 0, buffer.Length, token);
                if (readSize == 0)
                    break;
            }
        }
        
        /// <summary>
        /// Gets the header stream containing the zip central directory signature. 
        /// </summary>
        /// <returns>The <see cref="MemoryStream"/> containing the header data, or null if no header was found.</returns>
        public MemoryStream GetHeaderStream()
        {
            if (_centralDirectoryStream == null)
            {
                ModioLog.Error?.Log("No zip central directory signature found in the stream.");
                return null;
            }
            
            _centralDirectoryStream.Seek(0, SeekOrigin.Begin);
            return _centralDirectoryStream;
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_centralDirectoryStream == null)
                return;

            _centralDirectoryStream.Dispose();
            _centralDirectoryStream = null;
        }

        
        public override int Read(byte[] buffer, int offset, int count)
        {
            // If we have local entries and no central directory stream, we are reading the local data
            if (!_noMoreLocalEntries && _centralDirectoryStream == null)
                return base.Read(buffer, offset, count);
            
            // need to read from the raw buffer
            int read = ReadRawBufferFixed(inputBuffer, buffer, offset, count);
            
            //start reading from the offset
            int readOffset = offset;
            
            // If we have no more local entries and no central directory stream, we need to seek the header in the buffer
            if(_noMoreLocalEntries && _centralDirectoryStream == null)
            {
                // If we are not reading headers, we need to seek the header in the buffer
                int headerLocation = SeekHeader(buffer, offset, read);
                if (headerLocation >= 0)
                {
                    //found the header, so we can start reading from there
                    _centralDirectoryStream = new MemoryStream();
                    readOffset = headerLocation;
                }
            }
            // If we have a central directory stream, we need to store the data until the end of the stream
            
            StoreUntilEnd(buffer, readOffset, read);
            
            return read;

        }

        /// <summary>
        /// Very similar to <see cref="InflaterInputBuffer.ReadRawBuffer(byte[])"/>, but it doesn't return 0 when it shouldn't
        /// </summary>
        static int ReadRawBufferFixed(InflaterInputBuffer buffer, byte[] outBuffer, int offset, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            int currentOffset = offset;
            int currentLength = length;

            while (currentLength > 0)
            {
                if (buffer.Available <= 0)
                {
                    buffer.Fill();
                    if (buffer.Available <= 0)
                        break;
                }
                int toCopy = Math.Min(currentLength, buffer.Available);
                Array.Copy(buffer.RawData, buffer.RawLength - buffer.Available, outBuffer, currentOffset, toCopy);
                currentOffset += toCopy;
                currentLength -= toCopy;
                buffer.Available -= toCopy;
            }
            return length - currentLength;
        } 
        
        int SeekHeader(byte[] buffer, int offset, int count)
        {
            int offsetToHeader = -1;
            // Read until we hit the end of the central directory
            for (int i = offset; i < count; i++)
            {
                byte currentByte = buffer[i];
                UpdateHeaderBytes(currentByte);
                
                int signature = ReadLeInt(_headerBytes, 0);
                
                // If we hit the end of the central directory, we can stop reading
                if (signature is not ZipConstants.CentralHeaderSignature)
                    continue;

                offsetToHeader = i - 3;
                break;
            }

            return offsetToHeader;
        }
        
        /// <summary>
        /// Read an <see cref="short"/> in little endian byte order.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="offset">The offset in the byte array where the long starts.</param>
        /// <returns>The short value read.</returns>
        int ReadLeShort(byte[] data, int offset) => data[offset] | (data[offset + 1] << 8);

        /// <summary>
        /// Read an <see cref="int"/> in little endian byte order.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="offset">The offset in the byte array where the long starts.</param>
        /// <returns>The int value read.</returns>
        int ReadLeInt(byte[] data, int offset) => ReadLeShort(data, offset) | (ReadLeShort(data, offset + 2) << 16);

        /// <summary>
        /// Read a <see cref="long"/> in little endian byte order.
        /// </summary>
        /// <param name="data">The byte array containing the data.</param>
        /// <param name="offset">The offset in the byte array where the long starts.</param>
        /// <returns>The long value read.</returns>
        long ReadLeLong(byte[] data, int offset)
            => (uint)ReadLeInt(data, offset) | ((long)ReadLeInt(data, offset + 4) << 32);
    }
}
