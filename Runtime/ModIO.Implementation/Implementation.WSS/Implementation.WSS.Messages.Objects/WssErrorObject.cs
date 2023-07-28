using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.Wss.Messages.Objects
{
	[System.Serializable]
	internal struct WssErrorObject
	{
		// Use the API Object for children
		public Error error;
		public string operation;
	}
}
