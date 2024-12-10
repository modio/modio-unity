using System.IO;
using UnityEngine.Networking;

namespace ModIO.Implementation.API
{
    internal class DownloadHandlerStream : DownloadHandlerScript
    {
        const int BufferSize = 1024*1024;

        readonly Stream _writeTo;

        ulong _contentLength;
        ulong _bytesRecieved;

        public DownloadHandlerStream(Stream writeTo) : base(new byte[BufferSize])
        {
            _writeTo = writeTo;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || data.Length < 1)
                return false;

            _writeTo.Write(data, 0, dataLength);
            _bytesRecieved += (ulong)dataLength;
            return true;
        }

        protected override void ReceiveContentLengthHeader(ulong contentLength)
        {
            _contentLength = contentLength;
            base.ReceiveContentLengthHeader(contentLength);
        }

        protected override float GetProgress()
        {
            if (_contentLength == 0) return 0;
            return _bytesRecieved / (float)_contentLength;
        }
    }
}
