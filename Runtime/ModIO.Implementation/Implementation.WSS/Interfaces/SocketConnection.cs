
#if UNITY_STANDALONE || UNITY_SWITCH || UNITY_GAMECORE || UNITY_PS4 || UNITY_PS5 || UNITY_ANDROID || UNITY_IOS || (MODIO_COMPILE_ALL && UNITY_EDITOR) || UNITY_WSA || !UNITY_2019_4_OR_NEWER
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModIO.Implementation.Wss.Messages;
using Newtonsoft.Json;

namespace ModIO.Implementation.Wss
{
	internal class SocketConnection : ISocketConnection
	{
		ClientWebSocket webSocket;
		readonly Mutex _sending = new Mutex();

		Action<WssMessages> Receive { get; set; }
		Action Disconnect { get; set; }
		bool closingConnection = false;
		public bool Connected() => webSocket?.State == WebSocketState.Open;

		public async Task<Result> SetupConnection(string url, Action<WssMessages> onReceive, Action onDisconnect)
		{
			Logger.Log(LogLevel.Error, $"[Socket] Setting up connection for WebSocket ({url})");
			
			// Check if we are already connected
			if(Connected())
			{
				return ResultBuilder.Success;
			}
			
			webSocket = new ClientWebSocket();
			webSocket.Options.UseDefaultCredentials = true;
			webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);

			try
			{
				var uri = new Uri(url);
				await webSocket.ConnectAsync(uri, CancellationToken.None);
			}
			catch(Exception e)
			{
				Logger.Log(LogLevel.Error, $"[Socket] Failed to connect WSS Gateway."
				                           + $"\nException: {e.Message}");
				return ResultBuilder.Create(ResultCode.WSS_NotConnected);
			}
			
			Logger.Log(LogLevel.Verbose, $"[Socket] WSS Gateway connected");

			Receive = onReceive;
			Disconnect = onDisconnect;
			ReceiveMessages();
			
			return ResultBuilder.Success;
		}

		public async Task CloseConnection()
		{
			if (Connected())
			{
				try
				{
					closingConnection = true;
					await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
					webSocket.Dispose();
				}
				catch(Exception e)
				{
					Logger.Log(LogLevel.Error, $"[Socket] Failed to close the WebSocket connection. Exception: {e.Message}");
				}
				finally
				{
					closingConnection = false;
					webSocket = null;
					Logger.Log(LogLevel.Error, $"[Socket] CLOSED");
				}
			}
		}

		async void ReceiveMessages()
		{
			byte[] buffer = new byte[4096];

			while(webSocket.State == WebSocketState.Open)
			{
				try
				{
					WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

					// process into json string
					byte[] receivedData = new byte[result.Count];
					Array.Copy(buffer, receivedData, result.Count);
					string message = Encoding.UTF8.GetString(receivedData);

					Logger.Log(LogLevel.Verbose, $"[Socket] RECEIVED [{result.MessageType.ToString()}"
					                             + $":{webSocket.State.ToString()}"
					                             + $"{(result.CloseStatusDescription != null ? $":\"{result.CloseStatusDescription}\"" : "")}]"
					                             + $"\nmessage: {message}");

					if(result.MessageType == WebSocketMessageType.Close
					   || webSocket.State == WebSocketState.CloseReceived)
					{
						Disconnect?.Invoke();
						break;
					}

					// TODO write check for unexpected object structures
					if (result.MessageType == WebSocketMessageType.Text)
					{
						var messages = JsonConvert.DeserializeObject<WssMessages>(message);
						Receive?.Invoke(messages);
					}

					// Give roughly a frame of delay between receiving new messages
					await Task.Delay(16);
				}
				catch(Exception e)
				{
					Logger.Log(LogLevel.Error, $"[Socket] Exception caught during SocketConnection.Receive."
					                           // + $" Closing connection."
					                           + $"\n{e.Message}\nStacktrace: {e.StackTrace}");
					if(!closingConnection)
					{
						if(Connected())
						{
							await CloseConnection();
						}
						Disconnect?.Invoke();
					}
					break;
				}
			}
		}

		public async Task<Result> SendData(WssMessages message)
		{
			if(!Connected())
			{
				return ResultBuilder.Create(ResultCode.WSS_NotConnected);
			}
			
			_sending.WaitOne();
			try
			{
				byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
				await webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
			}
			catch(Exception e)
			{
				Logger.Log(LogLevel.Error, $"[Socket] Failed to send data across the WSS Gateway."
				                           + $"\nException: {e.Message}");
				return ResultBuilder.Create(ResultCode.WSS_FailedToSend);
			}
			finally
			{
				_sending.ReleaseMutex();
			}
			
			return ResultBuilder.Success;
		}
	}
}
#endif