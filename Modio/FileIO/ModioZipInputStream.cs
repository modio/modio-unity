using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Modio.FileIO
{
    public class ModioZipInputStream : ZipInputStream
    {
        public long CentralDirectoryStreamOffset { get; private set; }
        MemoryStream _centralDirectoryStream;
        readonly byte[] _headerBytes = new byte[4];
        readonly byte[] _buffer;
        bool _noMoreLocalEntries;

        public ModioZipInputStream(Stream baseInputStream, int bufferSize = 4096) : base(baseInputStream, bufferSize) => _buffer = new byte[bufferSize];

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

            long startPosition = Position - inputBuffer.Available;

            ZipEntry output = GetNextEntry();

            if (output != null)
            {
                output.Offset = startPosition;
                return output;
            }

            int offset = inputBuffer.RawLength - inputBuffer.Available;

            if (offset < 4)
                for (var i = 0; i < offset; i++)
                    UpdateHeaderBytes(inputBuffer.RawData[i]);
            else
                Buffer.BlockCopy(inputBuffer.RawData, offset - 4, _headerBytes, 0, 4);

            _noMoreLocalEntries = true;

            CentralDirectoryStreamOffset = Position - inputBuffer.Available - _headerBytes.Length;
            
            _centralDirectoryStream = new MemoryStream();
            _centralDirectoryStream.Write(_headerBytes);

            return null;
        }

        public async Task ReadUntilEndAsync(CancellationToken token)
        {
            while (true)
            {
                int readSize = await ReadAsync(_buffer, 0, _buffer.Length, token);

                if (readSize == 0)
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_centralDirectoryStream == null)
                return;

            _centralDirectoryStream.Dispose();
            _centralDirectoryStream = null;
        }

        public IReadOnlyList<ZipEntry> GetEocdEntries()
        {
            FindEndOfCentralRecordInfo(out long recordCount);

            var entries = new List<ZipEntry>((int)Math.Min(int.MaxValue, recordCount));
            using var zipHelperStream = new ZipHelperStream(_centralDirectoryStream);
            
            int recordsRead = 0;
            while (zipHelperStream.ReadEntry() is { } entry)
            {
                if (recordsRead++ >= recordCount)
                {
                    ModioLog.Warning?.Log($"Read the expected number of records already in zip; ignoring {entry.Name}");
                    continue;
                }

                entries.Add(entry);
            }

            if (recordsRead != recordCount)
                ModioLog.Warning?.Log($"Didn't see the expected number of records in the zip; {recordsRead} < {recordCount}");

            return entries;
        }
        
        public void FindEndOfCentralRecordInfo(out long recordCount)
        {
            if(_centralDirectoryStream == null)
                    throw new ZipException("No data found after files; headers have not been read");
            
            //Assume the last thing in the file is a commentless EOCR
            long headerPos = _centralDirectoryStream.Length - ZipConstants.EndOfCentralRecordBaseSize;
            int headerSignature = 0;

            _centralDirectoryStream.Seek(headerPos, SeekOrigin.Begin);

            //Read the initial 4 bytes at this position
            for (int i = 0; i < 4; i++)
                headerSignature |= _centralDirectoryStream.ReadByte() << (8*i);


            // Move backwards from (near) the end of the file until we find the header signature 
            while (true)
            {
                //If we find the signature, we still need to verify it's got valid offsets
                if (headerSignature == ZipConstants.EndOfCentralDirectorySignature)
                {
                    //Start of CD offset is stored at header+16
                    _centralDirectoryStream.Seek(headerPos + 16, SeekOrigin.Begin);
                    long possibleOffset = ReadIntFromCdStream();

                    if (possibleOffset == ~0 || (possibleOffset >= 0
                                                 && possibleOffset
                                                 < headerPos + CentralDirectoryStreamOffset - ZipConstants.CentralHeaderBaseSize))
                    {
                        //We've confirmed this is a valid EOCD, get the other useful info from it
                        _centralDirectoryStream.Seek(headerPos + 10, SeekOrigin.Begin);
                        recordCount = ReadShortFromCdStream();

                        long eocdLocatorPos = headerPos - ZipConstants.Zip64EndOfCentralDirectoryLocatorSize;
                        _centralDirectoryStream.Seek(eocdLocatorPos, SeekOrigin.Begin);
                        var isZip64 = ReadIntFromCdStream() == ZipConstants.Zip64CentralDirLocatorSignature;

                        if (!isZip64 && (possibleOffset == ~0 || recordCount == 0xFFFF))
                        {
                            throw new ZipException("Zip64 end of Central Directory Signature not found, but records indicated it would be");
                        }

                        if (isZip64)
                        {
                            _centralDirectoryStream.Seek(eocdLocatorPos + 8, SeekOrigin.Begin);
                            long eocdLocation = ReadLongFromCdStream() - CentralDirectoryStreamOffset;
                            
                            _centralDirectoryStream.Seek(eocdLocation, SeekOrigin.Begin);
                            
                            //verify we're at an EOCD64
                            int readSignature = ReadIntFromCdStream();
                            isZip64 = readSignature == ZipConstants.Zip64CentralFileHeaderSignature;
                            if(!isZip64)
                                throw new ZipException("EOCD64 not found at position indicated by EOCDL");
                            
                            //Read Offset Size
                            //     32	  8	    Total number of central directory records.
                            //     48	  8	    Offset of start of central directory, relative to start of archive.
                            _centralDirectoryStream.Seek(eocdLocation + 32, SeekOrigin.Begin);
                            recordCount = ReadLongFromCdStream();
                            _centralDirectoryStream.Seek(eocdLocation + 48, SeekOrigin.Begin);
                            possibleOffset = ReadLongFromCdStream();

                            if (possibleOffset < 0
                                || possibleOffset
                                > eocdLocation + CentralDirectoryStreamOffset - ZipConstants.CentralHeaderBaseSize)
                            {
                                throw new ZipException($"EOCD64 indicates impossible offset {eocdLocation} (outside bounds {0} to { eocdLocation + CentralDirectoryStreamOffset - ZipConstants.CentralHeaderBaseSize})");
                            }
                        }
                        
                        if(possibleOffset - CentralDirectoryStreamOffset < 0)
                                throw new ZipException($"Unable to process zip: {possibleOffset - CentralDirectoryStreamOffset} is negative, indicating partial files");
                        
                        _centralDirectoryStream.Seek(
                            possibleOffset - CentralDirectoryStreamOffset,
                            SeekOrigin.Begin
                        );
                        
                        return;
                    }
                }

                //We couldn't find the header
                if (--headerPos == 0)
                    throw new ZipException("No EOCD signature found");

                _centralDirectoryStream.Seek(headerPos, SeekOrigin.Begin);

                //We've got 3 of the bytes already, just slot in the new one
                headerSignature <<= 8;
                headerSignature |= _centralDirectoryStream.ReadByte();
            }
        }

        short ReadShortFromCdStream()
        {
            short value = 0;
            for (int i = 0; i < 2; i++)
                value |= (short)(_centralDirectoryStream.ReadByte() << (8 * i));

            return value;
        }

        int ReadIntFromCdStream()
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
                value |= _centralDirectoryStream.ReadByte() << (8 * i);
            return value;
        }

        long ReadLongFromCdStream() => (uint)ReadIntFromCdStream() | ((long)(uint)ReadIntFromCdStream() << 32);

        public override int Read(byte[] buffer, int offset, int count)
        {
            // If we have local entries and no central directory stream, we are reading the local data
            if (_centralDirectoryStream == null)
                return base.Read(buffer, offset, count);

            // need to read from the raw buffer
            int read = ReadRawBufferFixed(inputBuffer, buffer, offset, count);

            // If we have a central directory stream, we need to store the data until the end of the stream
            _centralDirectoryStream.Write(buffer, offset, read);

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
    }
}
