using System;
using System.Threading.Tasks;
using ModIO.Implementation.Wss.Messages;
using ModIO.Implementation.Wss.Messages.Objects;
using UnityEngine;

namespace ModIO.Implementation.Wss
{
	/// <summary>
	/// This is the main class for interacting with any WSS behavior.
	/// </summary>
	internal static class Wss
	{

		/// <summary>
		/// Sends a request to the server to begin a multi device login process. Once it receives
		/// the 5 digit code from the server it will return the token with said code and url to
		/// display for the user.
		/// </summary>
		/// <seealso cref="ExternalAuthenticationToken"/>
		/// <returns>(If result succeeds) returns a token with the code and url, as well as a
		/// Task that can be awaited for when the login is successful</returns>
		public static async Task<ResultAnd<ExternalAuthenticationToken>> BeginAuthenticationProcess(bool restartProcess = false)
		{
			// Create setup message
			WssMessage message = WssRequest.DeviceLogin();

			// Wait for first response, sort of like a handshake
			var handshake = await WssHandler.DoMessageHandshake<WssDeviceLoginResponse>(message);

			if(!handshake.result.Succeeded())
			{
				// FAILURE
				return ResultAnd.Create<ExternalAuthenticationToken>(handshake.result, default);
			}

			// wait for ongoing message (put this task into the token to be awaited)
			var task = WaitForAccessToken();

			// Create token to return to user once we've sent our initial message and received the 5 digit response
			ExternalAuthenticationToken token = new ExternalAuthenticationToken
			{
				code = handshake.value.code,
				url = handshake.value.login_url,
				autoUrl = $"{handshake.value.login_url}?code={handshake.value.code}",
				expiryTime = DateTimeOffset.FromUnixTimeSeconds(handshake.value.date_expires).DateTime,
				task = task,
				cancel = ()=> WssHandler.CancelWaitingFor(WssOperationType.Wss_AccessToken)
			};

			return ResultAnd.Create(ResultBuilder.Success, token);
		}

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
		/// Begins listening for the WssMessage for the multi device authentication attempt.
		/// This will timeout after 15 minutes.
		/// </summary>
		/// <returns>the result of the authentication</returns>
		static async Task<Result> WaitForAccessToken()
		{
			var response = await WssHandler.WaitForMessage(WssOperationType.Wss_AccessToken);

			Result result = response.result;
			if(result.Succeeded())
			{
				if(response.value.TryGetValue<WssLoginSuccess>(out var token))
				{
					try
					{
						UserData.instance.SetOAuthToken(token);

						// HACK this is kind of hacky, but we're not given the user profile, we need
						// to silently retrieve it
						await ModIOUnityImplementation.GetCurrentUser(delegate { });
					}
					catch(Exception e)
					{
						Logger.Log(LogLevel.Error, $"Internal: Failed to deserialize user/token "
						                           + $"object from WssMessage and assign to UserData."
						                           + $"\n{e.Message}\nStacktrace: {e.StackTrace}");
						result = ResultBuilder.Create(ResultCode.Internal_FailedToDeserializeObject);
					}
				}
				else
				{
					result = ResultBuilder.Create(ResultCode.WSS_UnexpectedMessage);
				}
			}

			// TODO HACK REMOVE THIS - DISCONNECT THE SOCKET
#pragma warning disable 4014
			//----------------------------------------------
			// currently there are no other events or listeners for the socket to be used for, thus
			// we can disconnect it when we've finished the authentication flow (For now)
			// We will need to remove this next line once more features are added to the Wss Socket
			WssHandler.Shutdown();
			//----------------------------------------------
#pragma warning restore 4014

			return result;
		}
	}
}
