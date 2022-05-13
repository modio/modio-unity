namespace ModIO.Implementation.API
{
    class BinaryDataContainer
    {
        public string key;
        public string fileName;
        public byte[] data;
        public BinaryDataContainer(string key, string fileName, byte[] data)
        {
            this.key = key;
            this.fileName = fileName;
            this.data = data;
        }
    }
}
