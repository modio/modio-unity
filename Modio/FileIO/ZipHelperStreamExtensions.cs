using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace Modio.FileIO
{
    public static class ZipHelperStreamExtensions
    {
        static ushort ReadLeUshort(this ZipHelperStream stream)
            => unchecked((ushort)((ushort)stream.ReadByte() | (ushort)(stream.ReadByte() << 8)));

        static uint ReadLeUint(this ZipHelperStream stream)
            => (uint)(stream.ReadLeUshort() | (stream.ReadLeUshort() << 16));

        static ulong ReadLEUlong(this ZipHelperStream stream)
            => stream.ReadLeUint() | ((ulong)stream.ReadLeUint() << 32);

        internal static ZipEntry ReadEntry(this ZipHelperStream stream)
        {
            uint signature = stream.ReadLeUint();

            if (signature != ZipConstants.CentralHeaderSignature)
                return null;

            int versionMadeBy = stream.ReadLeUshort();
            int versionToExtract = stream.ReadLeUshort();
            int bitFlags = stream.ReadLeUshort();
            int method = stream.ReadLeUshort();

            uint dostime = stream.ReadLeUint();
            uint crc = stream.ReadLeUint();

            var csize = (long)stream.ReadLeUint();
            var size = (long)stream.ReadLeUint();

            int nameLen = stream.ReadLeUshort();
            int extraLen = stream.ReadLeUshort();
            int commentLen = stream.ReadLeUshort();

            int diskStartNo = stream.ReadLeUshort();        // Not currently used
            int internalAttributes = stream.ReadLeUshort(); // Not currently used

            uint externalAttributes = stream.ReadLeUint();
            long offset = stream.ReadLeUint();

            byte[] buffer = new byte[Math.Max(nameLen, commentLen)];

            stream.Read(buffer, 0, buffer.Length);

            string name = ZipStrings.ConvertToStringExt(bitFlags, buffer, nameLen);

            var entry = new ZipEntry(name, versionToExtract, versionMadeBy, (CompressionMethod)method)
            {
                Crc = crc & 0xffffffffL,
                Size = size & 0xffffffffL,
                CompressedSize = csize & 0xffffffffL,
                Flags = bitFlags,
                DosTime = dostime,
                ZipFileIndex = -1,
                Offset = offset,
                ExternalFileAttributes = (int)externalAttributes
            };

            if ((bitFlags & 8) == 0)
            {
                entry.CryptoCheckValue = (byte)(crc >> 24);
            }
            else
            {
                entry.CryptoCheckValue = (byte)((dostime >> 8) & 0xff);
            }

            if (extraLen > 0)
            {
                var extra = new byte[extraLen];
                stream.Read(extra, 0, extraLen);
                entry.ExtraData = extra;
            }

            entry.ProcessExtraData(false);

            if (commentLen > 0)
            {
                stream.Read(buffer, 0, commentLen);
                entry.Comment = ZipStrings.ConvertToStringExt(bitFlags, buffer, commentLen);
            }

            return entry;
        }
    }
}
