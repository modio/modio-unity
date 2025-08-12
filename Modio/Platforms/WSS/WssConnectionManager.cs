using System;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Modio.Errors;

namespace Modio.Platforms.Wss
{
    public class WssConnectionManager
    {
        WssService _wssService;
        internal ClientWebSocket WebSocket;
        readonly string _serverUrl;

        WssSettings Settings { get; }

        public WssConnectionManager(WssSettings settings)
        {
            Settings = settings;
            _serverUrl = Settings.ServerURL;

            if (!string.IsNullOrEmpty(_serverUrl))
                return;

            Match matches = Regex.Match(ModioClient.Settings.ServerURL, "https://(?<gameid>g-[0-9]+).(?<domain>.+).io");
            _serverUrl = $"wss://{matches.Groups["gameid"].Value}.ws.{matches.Groups["domain"].Value}.io/";
        }

        internal bool IsConnected => WebSocket?.State == WebSocketState.Open;

        internal async Task<Error> Connect()
        {
            WebSocket = new ClientWebSocket();
            WebSocket.Options.UseDefaultCredentials = true;
            WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            var uri = new Uri(_serverUrl);
            try
            {
                await WebSocket.ConnectAsync(uri, CancellationToken.None);
            }
            catch (WebSocketException)
            {
                return new WssError(ErrorCode.WSS_SERVICE_NOT_CONNECTED);
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log($"[WSS] Unexpected error while connecting to WSS Gateway: {e.Message}");
                return Error.Unknown;
            }

            return Error.None;
        }


        internal async Task Disconnect()
        {
            //already disconnected
            if (!IsConnected)
                return;

            await WebSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closing connection",
                CancellationToken.None
            );

            WebSocket.Dispose();
            WebSocket = null;
        }
    }
}
