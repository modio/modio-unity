using System.Threading.Tasks;

namespace ModIO.Implementation
{
    /// <summary>Stream implementation used in ModIO.</summary>
    internal abstract class ModIOFileStream : System.IO.Stream
    {
        /// <summary>Gets the absolute path of the file opened in the stream.</summary>
        public abstract string FilePath { get; }

        public abstract Task<ResultAnd<byte[]>> ReadAllBytesAsync();
        public abstract ResultAnd<byte[]> ReadAllBytes();
        public abstract Task<Result> WriteAllBytesAsync(byte[] buffer);
        public abstract Result WriteAllBytes(byte[] buffer);
    }
}
