using System.IO;
using UnityEngine.Networking;

namespace ModIO.Implementation.API
{
    internal class DownloadHandlerStream : DownloadHandlerScript
    {
        const int BufferSize = 1024*1024;

        readonly Stream _writeTo;
        public DownloadHandlerStream(Stream writeTo) : base(new byte[BufferSize])
        {
            _writeTo = writeTo;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            _writeTo.Write(data, 0, dataLength);
            return true;
        }
    }
}
