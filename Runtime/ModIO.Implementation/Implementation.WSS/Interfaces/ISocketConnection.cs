using System;
using System.Threading.Tasks;
using ModIO.Implementation.Wss.Messages;

namespace ModIO.Implementation.Wss
{
	internal interface ISocketConnection
	{
		/// <summary>
		/// Used to check if the socket currently has an open connection in use
		/// </summary>
		bool Connected();
		/// <summary>
		/// Used to send a WssMessage struct to the server
		/// </summary>
		/// <param name="message"></param>
		/// <seealso cref="WssMessage"/>
		/// <returns>If the data was able to send or not</returns>
		Task<Result> SendData(WssMessages message);
		/// <summary>
		/// Used to setup the socket initially with the provided url
		/// </summary>
		/// <param name="url"></param>
		/// <param name="onReceiveMessage">invoked each time the socket receives a WssMessage
		/// from the API server</param>
		/// <param name="onDisconnect">invoked when the socket disconnects.
		/// This is not invoked when manually closing the connection <see cref="CloseConnection"/></param>
		/// <seealso cref="WssMessage"/>
		/// <returns>If the connection was established or not</returns>
		Task<Result> SetupConnection(string url, Action<WssMessages> onReceiveMessage, Action onDisconnect);
		/// <summary>
		/// Closing the connection manually wont invoke the above 'onDisconnect' message.
		/// Instead, use the Task and await the disconnect.
		/// </summary>
		Task CloseConnection();
	}
}
