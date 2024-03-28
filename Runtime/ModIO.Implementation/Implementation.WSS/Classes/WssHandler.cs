using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ModIO.Implementation.Wss.Messages;
using ModIO.Implementation.Wss.Messages.Objects;

namespace ModIO.Implementation.Wss
{
    /// <summary>
    /// This class primarily handles the WSS connection and listens/sends data between the
    /// client and server.
    /// </summary>
    internal static class WssHandler
    {
        static string GatewayUrl => $"wss://g-{Settings.server.gameId}.ws.{Regex.Match(Settings.server.serverURL, "https://[^.]+.(?<domain>.+).io").Groups["domain"]}.io/";

        // TODO set this up in a partial class and duck type it based on platform
        static ISocketConnection Socket = new SocketConnection();

        /// <summary>
        /// The <see cref="WaitForMessage"/> method adds an entry into this dictionary. T
        /// he <see cref="Receive"/> method completes the TCS value.
        /// Setting the TCS result to 'default' will return a cancelled result type to listeners.
        /// </summary>
        /// <remarks>Always remove the a KVP before using SetResult() on the TCS</remarks>
        static Dictionary<string, TaskCompletionSource<WssMessage>> WaitingForMessages = new Dictionary<string, TaskCompletionSource<WssMessage>>();
        /// <summary>
        /// These are ongoing listeners that receive WssMessages of a specific operation type continually until they're unsubscribed
        /// </summary>
        /// TODO Not Implemented Yet (The current API feature set is limited to MultiDeviceLogin - this is scoped for potential future features)
        static Dictionary<string, Action<WssMessage>> SubscribedMessageListeners = new Dictionary<string, Action<WssMessage>>();
        // If nothing was listening or expecting a message, we cache it until a message of the same operation type overrides it
        static Dictionary<string, WssMessage> UnhandledMessages = new Dictionary<string, WssMessage>();

        /// <summary>
        /// Waits for a WssMessage with the specified operation type.
        /// </summary>
        /// <param name="messageOperation"></param>
        /// <param name="checkPreviousUnhandledMessages">if true, this will check previous messages </param>
        /// <see cref="WssMessage"/>
        /// <returns></returns>
        public static async Task<ResultAnd<WssMessage>> WaitForMessage(string messageOperation, bool checkPreviousUnhandledMessages = false)
        {
            if(checkPreviousUnhandledMessages)
            {
                if(UnhandledMessages.ContainsKey(messageOperation))
                {
                    var foundPrevious = ResultAnd.Create(ResultBuilder.Success, UnhandledMessages[messageOperation]);
                    UnhandledMessages.Remove(messageOperation);
                    return foundPrevious;
                }
            }
            TaskCompletionSource<WssMessage> tcs = new TaskCompletionSource<WssMessage>();
            while(WaitingForMessages.ContainsKey(messageOperation))
            {
                // Wait for the current awaiter to finish
                await WaitingForMessages[messageOperation].Task;
            }
            
            // This should be safe as it removes the TCS from the table before it completes
            WaitingForMessages.Add(messageOperation, tcs);
                
            // Wait for the message to arrive (or timeout)
            await Task.WhenAny(tcs.Task, Task.Delay(900000)); // 15 minute timeout
            
            Result result = tcs.Task.IsCompleted ? ResultBuilder.Success : ResultBuilder.Create(ResultCode.WSS_MessageTimeout);
            WssMessage message = tcs.Task.IsCompleted ? tcs.Task.Result : default;

            if(result.code == ResultCode.Success && string.IsNullOrEmpty(message.operation))
            {
                // If the message is default but we didn't timeout, it was a forced cancel
                result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
            }
            return ResultAnd.Create(result, message);
        }

        public static async Task<ResultAnd<T>> DoMessageHandshake<T>(WssMessage message) where T : struct
        {
            // Create the listener first, in case a thread yield makes us miss the socket message (Very unlikely)
            var task = WaitForMessage(WssOperationType.Wss_DeviceLogin);
            
            // Send the initial WssMessage
            Result result = await Send(message);
            Logger.Log(LogLevel.Verbose, $"[Socket] SENT ({message.operation})");
            
            if(!result.Succeeded())
            {
                Logger.Log(LogLevel.Verbose, $"[Socket] Failed to send");
                // FAILED - FAILED TO SEND
                return ResultAnd.Create<T>(result, default);
            }
            
            // Wait for first response
            Logger.Log(LogLevel.Verbose, $"[Socket] Waiting for initial response");
            var responseMessage = await task;
            
            if(responseMessage.result.Succeeded())
            {
                Logger.Log(LogLevel.Verbose, $"[Socket] deserializing initial response");
                if(responseMessage.value.TryGetValue(out T context))
                {
                    // SUCCESS
                    return ResultAnd.Create(ResultBuilder.Success, context);
                }
                Logger.Log(LogLevel.Verbose, $"[Socket] Failed to deserialize");
                // FAILED TO DESERIALIZE
                return ResultAnd.Create<T>(ResultCode.WSS_UnexpectedMessage, default);
            }
            // FAILED - RESPONSE TIMED OUT OR CANCELLED
            return ResultAnd.Create<T>(responseMessage.result, default);
        }

        public static void CancelWaitingFor(string messageOperation)
        {
            if(WaitingForMessages.ContainsKey(messageOperation))
            {
                var tcs = WaitingForMessages[messageOperation];
                WaitingForMessages.Remove(messageOperation);
                tcs.SetResult(default);
            }
        }

        static void CancelAllAwaitingMessages()
        {
            List<TaskCompletionSource<WssMessage>> awaiters = WaitingForMessages.Values.ToList();
            WaitingForMessages.Clear();
            foreach(var tcs in awaiters)
            {
                tcs.SetResult(default);
            }
        }
        
        public static async Task Shutdown()
        {
            // Close socket
            await Socket.CloseConnection();
            
            // Clear cached messages and cancel listeners
            CancelAllAwaitingMessages();
            UnhandledMessages.Clear();

            // Yield once so listeners can return their cancelled results
            await Task.Yield();
        }
        
#region Direct Socket interactions and callbacks

        static async Task<Result> EnsureConnection()
        {
            // check connection
            if (!Socket.Connected())
            {
                return await Socket.SetupConnection(GatewayUrl, Receive, Disconnected);
            }
            return ResultBuilder.Success;
        }
        
        public static async Task<Result> Send(WssMessage message)
        {
            WssMessages messages = new WssMessages(message);
            
            Result result = await EnsureConnection();
            if(!result.Succeeded())
            {
                // FAILURE
                return result;
            }

            result = await Socket.SendData(messages);
            
            return result;
        }

        static void Receive(WssMessages messages)
        {
            foreach(var message in messages.messages)
            {
                // Check if this is an error message (we handle these a bit more uniquely)
                if(message.operation == WssOperationType.Wss_FailedOperation)
                {
                    ProcessErrorObject(message);
                    continue;
                }
                
                // TODO Find corresponding listener to inform in SubscribedMessageListener
                // ...

                // If not error, send the message to any listeners
                if(WaitingForMessages.ContainsKey(message.operation))
                {
                    var tcs = WaitingForMessages[message.operation];
                    WaitingForMessages.Remove(message.operation);
                    tcs.SetResult(message);
                }
                else
                {
                    // If there aren't any listeners, we cache it as the last received message of that operation type
                    Logger.Log(LogLevel.Verbose, $"[Socket] Received unexpected message "
                                                 + $"operation ({message.operation}).\nCaching it "
                                                 + $"temporarily in case we listen for it immediately after.");
                    
                    if(UnhandledMessages.ContainsKey(message.operation))
                    {
                        UnhandledMessages[message.operation] = message;
                    }
                    else
                    {
                        UnhandledMessages.Add(message.operation, message);
                    }
                }
            }
        }

        static void ProcessErrorObject(WssMessage message)
        {
            if(message.TryGetValue(out WssErrorObject errorObject))
            {
                Logger.Log(LogLevel.Error, "[Socket] Error received from WssMessages:\n"
                                           + $"Error: [{errorObject.error.code}]"
                                           + $" [{errorObject.error.error_ref}]"
                                           + $" {errorObject.error.message}");

                if(WaitingForMessages.ContainsKey(errorObject.operation))
                {
                    var tcs = WaitingForMessages[errorObject.operation];
                    WaitingForMessages.Remove(errorObject.operation);
                    // TODO change the TCS to take a wrapper for Result/bool for error and the WssMessage
                    // (or in WaitForMessage we check the object type for WssErrorObject
                    tcs.SetResult(default);
                }
                else if(SubscribedMessageListeners.ContainsKey(errorObject.operation))
                {
                    SubscribedMessageListeners[errorObject.operation]?.Invoke(message);
                    WaitingForMessages.Remove(errorObject.operation);
                    // TODO Need to simplify error handling for the listener
                }
                else
                {
                    Logger.Log(LogLevel.Warning, $"[Socket:Internal] Could not find any matching"
                                                 + $" listener for the error operation: {errorObject.operation}");
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, $"[Socket:Internal] Failed to cast WssMessage"
                                           + $" (operation \"{message.operation}\") into WssErrorObject");
            }
        }

        /// <summary>
        /// This only gets invoked if we did not manually request the connection to be closed
        /// </summary>
        static async void Disconnected()
        {
            // If we still have messages that are being waited for, try to re-connect
            if(WaitingForMessages.Count > 0)
            {
                Logger.Log(LogLevel.Warning, "[Socket] Disconnected while the WssHandler"
                                             + " was still waiting for messages. Attempting to"
                                             + " reconnect the WebSocket.");
                
                // We have to cancel any message awaiters because the connection closed unexpectedly
                // it's unintended that the server will still send the messages when a close message
                // has been sent (or network connection failed)
                CancelAllAwaitingMessages();
                
                Result result = await EnsureConnection();
                if(!result.Succeeded())
                {
                    Logger.Log(LogLevel.Error, "[Socket] Failed to re-establish Socket connection."
                                               + " Listeners for WssMessages have been cancelled");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "[Socket] Re-established Socket connection. "
                                                 + "Listeners for WssMessages have been cancelled");
                }
            }
        }
#endregion
    }
}
