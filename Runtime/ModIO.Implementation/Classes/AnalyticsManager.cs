using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModIO;
using ModIO.Implementation;
using Logger = ModIO.Implementation.Logger;
using Random = System.Random;

namespace Plugins.mod.io.Runtime.ModIO.Implementation.Classes
{
    internal static class AnalyticsManager
    {
        static readonly Random Rand = new Random();
        static readonly Dictionary<string, SessionData> SessionCache = new Dictionary<string, SessionData>();

        const int MinSecondsBetweenBeats = 5 * 60;
        const int MaxSecondsBetweenBeats = 6 * 60;

        public static readonly string MetricsSecret;

        static AnalyticsManager()
        {
            SettingsAsset.TryLoad(out MetricsSecret);
        }

        public static void AddSession(string sessionId, long[] modIds)
        {
            if (SessionCache.ContainsKey(sessionId))
            {
                SessionCache.Remove(sessionId);
            }

            SessionCache.Add(sessionId, new SessionData(sessionId, modIds));
        }

        public static bool IsSessionIdValid(string sessionId, out Result result)
        {
            if (sessionId != null && SessionCache.ContainsKey(sessionId))
            {
                result = ResultBuilder.Success;
                return true;
            }
            result = ResultBuilder.Unknown;
            return false;
        }

        public static SessionData GetSession(string sessionId)
        {
            if (!SessionCache.TryGetValue(sessionId, out var data))
                return null;

            return data;
        }
        
        public static void ShutdownAllAnalyticsSessions()
        {
            foreach (var sessionData in SessionCache.Values)
            {
                sessionData.HeartbeatCancellationToken?.Cancel();
            }
            SessionCache.Clear();
        }

        public static void RemoveAnalyticsSessionFromCache(string sessionId)
        {
            if (SessionCache.ContainsKey(sessionId) && SessionCache[sessionId].HeartbeatCancellationToken != null)
            {
                SessionCache[sessionId].HeartbeatCancellationToken.Cancel();
                SessionCache.Remove(sessionId);
            }
        }

        public static int IncreaseSessionOrderId(string sessionId)
        {
            if (!SessionCache.TryGetValue(sessionId, value: out SessionData value))
                return 0;

            return ++value.SessionOrderId;
        }

        public static void StartHeartbeat(string sessionId)
        {
            if (!SessionCache.TryGetValue(sessionId, out SessionData value))
            {
                Logger.Log(LogLevel.Error, "Session not found, cannot start heartbeat!");
                return;
            }

            if (value.HeartbeatCancellationToken != null)
            {
                Logger.Log(LogLevel.Warning, "Heartbeat has already been started, cancelling this request!");
                return;
            }
            
            value.HeartbeatCancellationToken = new CancellationTokenSource();
            Task.Run(()=>Heartbeat(sessionId, SessionCache[sessionId].HeartbeatCancellationToken.Token));
        }
        
        static async Task Heartbeat(string sessionId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int totalDelayInSeconds = Rand.Next(MinSecondsBetweenBeats, MaxSecondsBetweenBeats);
                Result result = await ModIOUnityImplementation.SendAnalyticsHeartbeat(sessionId);
                if (!result.Succeeded())
                {
                    Logger.Log(LogLevel.Warning, $"Heartbeat request failed, {result.apiMessage}");
                }
                
                int remainingTime = totalDelayInSeconds;
                while (remainingTime > 0 && !cancellationToken.IsCancellationRequested)
                {
                    int step = Math.Min(5, remainingTime);//in seconds
                    await Task.Delay(TimeSpan.FromSeconds(step));
                    remainingTime -= step;
                }
            }
        }

    }
}
