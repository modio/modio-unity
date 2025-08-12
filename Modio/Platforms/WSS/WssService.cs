using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Modio.Authentication;
using Modio.Errors;
using Modio.Extensions;
using Modio.Platforms.Wss.Operations;
using Modio.Wss.Messages;
using Newtonsoft.Json;

namespace Modio.Platforms.Wss
{
    public class WssService
    {
        
        readonly Mutex _sending = new Mutex();
        readonly Mutex _receiving = new Mutex();

        readonly byte[] _buffer = new byte[4096];
        
        bool _shuttingDown;
        WssSettings _platformSettings;

        WssSettings Settings { get; }
        WssMessageDispatcher WssMessageDispatcher { get; }
        WssConnectionManager WssConnectionManager { get; }
        
        public WssService()
        {
            if (!ModioClient.Settings.TryGetPlatformSettings(out WssSettings setting))
                ModioLog.Error?.Log(
                    "WSS Settings not found. Please ensure you have initialized the WSS platform settings."
                );

            Settings = setting;
            WssConnectionManager = new WssConnectionManager(Settings);
            WssMessageDispatcher = new WssMessageDispatcher();
        }
        

        internal async Task<(Error error, WssMessage message)> WaitForMessage(
            string messageOperation,
            bool checkPreviousUnhandledMessages = false
        )
        {
            Error error = Error.None;
            if (!WssConnectionManager.IsConnected)
                error = await StartService();

            if(error)
                return (error, default(WssMessage));
            
            TaskCompletionSource<WssMessage> tcs = await WssMessageDispatcher.WaitForMessages(
                messageOperation,
                checkPreviousUnhandledMessages
            );

            await Task.WhenAny(tcs.Task, Task.Delay(900000)); // 15 minute timeout

            error = tcs.Task.IsCompleted ? Error.None : new WssError(ErrorCode.WSS_TIMEOUT);

            WssMessage message = tcs.Task.IsCompleted ? tcs.Task.Result : default(WssMessage);

            if (!error && string.IsNullOrEmpty(message.operation))
                error = new Error(ErrorCode.OPERATION_CANCELLED);

            return (error, message);
        }

        async Task ProcessMessages()
        {
            while (WssConnectionManager.WebSocket.State == WebSocketState.Open)
            {
                _receiving.WaitOne();

                try
                {
                    WebSocketReceiveResult result = await WssConnectionManager.WebSocket.ReceiveAsync(
                        new ArraySegment<byte>(_buffer),
                        CancellationToken.None
                    );

                    // process into json string
                    var receivedData = new byte[result.Count];
                    Array.Copy(_buffer, receivedData, result.Count);
                    string message = Encoding.UTF8.GetString(receivedData);

                    ModioLog.Verbose?.Log(
                        $"[Socket] RECEIVED [{result.MessageType.ToString()}"
                        + $":{WssConnectionManager.WebSocket.State.ToString()}"
                        + $"{(result.CloseStatusDescription != null ? $":\"{result.CloseStatusDescription}\"" : "")}]"
                        + $"\nmessage: {message}"
                    );

                    if (result.MessageType == WebSocketMessageType.Close
                        || WssConnectionManager.WebSocket.State == WebSocketState.CloseReceived)
                    {
                        await WssConnectionManager.Disconnect();
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messages = JsonConvert.DeserializeObject<WssMessages>(message);
                        Receive(messages);
                    }

                    // Give roughly a frame of delay between receiving new messages
                    await Task.Delay(16);
                }
                catch (Exception e)
                {
                    ModioLog.Error?.Log(
                        $"[Socket] Exception caught during SocketConnection.Receive."
                        // + $" Closing connection."
                        + $"\n{e.Message}\nStacktrace: {e.StackTrace}"
                    );

                    await WssConnectionManager.Disconnect();

                    break;
                }
                finally
                {
                    _receiving.ReleaseMutex();
                }
            }
        }

        void OnShutdown()
        {
            WssMessageDispatcher.CancelAllAwaitingMessages();
            WssMessageDispatcher.UnhandledMessages.Clear();
            WssConnectionManager.Disconnect().ForgetTaskSafely();
        }

        void Receive(WssMessages messages)
        {
            foreach (WssMessage message in messages.messages)
            {
                // Check if this is an error message (we handle these a bit more uniquely)
                if (message.operation == WssOperationType.WSS_FAILED_OPERATION)
                    continue;

                if (WssMessageDispatcher.TryHandleMessage(message))
                    continue;

                WssMessageDispatcher.AddUnhandledMessage(message);
            }
        }

        internal async Task<(Error, TResponse)> DoMessageHandshake<TResponse>(string operation, WssMessage message)
            where TResponse : struct
        {
            if (!WssConnectionManager.IsConnected)
                await StartService();

            Task<(Error error, WssMessage message)> task = WaitForMessage(operation);

            Error error = await Send(message);

            if (error)
                return (error, default(TResponse));

            (Error error, WssMessage message) response = await task;

            if (response.error)
                return (response.error, default(TResponse));

            if (response.message.TryGetValue(out TResponse context))
                return (Error.None, context);

            return (new WssError(ErrorCode.WSS_FAILED_TO_DESERIALIZE), default(TResponse));
        }

        async Task<Error> Send(WssMessage message)
        {
            var messages = new WssMessages(message);

            if (!WssConnectionManager.IsConnected)
            {
                Error error = await WssConnectionManager.Connect();

                if (error)
                    return error;
            }

            if (!WssConnectionManager.IsConnected)
                return new WssError(ErrorCode.WSS_SERVICE_NOT_CONNECTED);

            _sending.WaitOne();

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messages));

                await WssConnectionManager.WebSocket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log(
                    $"[Socket] Failed to send data across the WSS Gateway."
                    + $"\nException: {e.Message}"
                );

                return new WssError(ErrorCode.WSS_FAILED_TO_SEND);
            }
            finally
            {
                _sending.ReleaseMutex();
            }

            return Error.None;
        }

        async Task<Error> StartService()
        {

            if (WssConnectionManager.IsConnected)
            {
                ModioLog.Error?.Log("[Socket] WSS Gateway is already connected.");
                return Error.None;
            }
            try
            {
                Error error = await WssConnectionManager.Connect();

                if (error)
                {
                    OnShutdown();
                    return error;
                }
                ProcessMessages().ForgetTaskSafely();
                ModioClient.OnShutdown += OnShutdown;
            }
            catch
            {
                ModioLog.Error?.Log("[Socket] Failed to connect to the WSS Gateway.");
                return new WssError(ErrorCode.WSS_SERVICE_NOT_CONNECTED);
            }

            return Error.None;
        }

        internal async Task StopService()
        {
            if (!WssConnectionManager.IsConnected && !_shuttingDown)
                return;
            
            // already shutting down
            if (_shuttingDown)
                return;
            
            try
            {
                _shuttingDown = true;
                WssMessageDispatcher.CancelAllAwaitingMessages();
                await WssConnectionManager.Disconnect();
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log($"[Socket] Failed to close the WebSocket connection. Exception: {e.Message}");
            }
            finally
            {
                _shuttingDown = false;
                ModioLog.Error?.Log($"[Socket] CLOSED");
                ModioClient.OnShutdown -= OnShutdown;
            }
        }
        
#region Debug Tools

        [ModioDebugMenu]
        public static void UseWssService()
        {
            if (!ModioServices.Resolve<ModioSettings>().TryGetPlatformSettings(out WssSettings _))
                ModioServices.Resolve<ModioSettings>().PlatformSettings = ModioServices.Resolve<ModioSettings>()
                                                                                       .PlatformSettings.Append(
                                                                                           new WssSettings()
                                                                                       ).ToArray();
            ModioServices.Bind<WssAuthService>()
                         .WithInterfaces<IModioAuthService>()
                         .WithInterfaces<IGetActiveUserIdentifier>()
                         .WithInterfaces<IGetPortalProvider>()
                         .FromNew<WssAuthService>(ModioServicePriority.DeveloperOverride + 10);
        }
#endregion
    }
}
