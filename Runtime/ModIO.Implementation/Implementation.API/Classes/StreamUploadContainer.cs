using System.IO;

namespace ModIO.Implementation.API
{
	class StreamUploadContainer
	{
		public string fieldName;
		public string fileName;
		public Stream data;
		public StreamUploadContainer(string fieldName, string fileName, Stream data)
		{
			this.fieldName = fieldName;
			this.fileName = fileName;
			this.data = data;
		}
	}
}
