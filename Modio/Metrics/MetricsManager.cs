using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Errors;
using Modio.Extensions;
using Modio.Users;

namespace Modio.Metrics
{
    public class MetricsManager
    {
        const int HEARTBEAT_INTERVAL = 150;

        readonly Dictionary<string, MetricsSession> _sessions = new Dictionary<string, MetricsSession>();

        string Secret => _settings == null ? string.Empty : _settings.Secret;

        readonly MetricsSettings _settings;

        public MetricsManager()
        {
            var settings = ModioServices.Resolve<ModioSettings>();
            _settings = settings.GetPlatformSettings<MetricsSettings>();

            if (string.IsNullOrEmpty(Secret))
            {
                ModioLog.Error?.Log("Metrics Secret has not been set.");
            }
        }

        /// <summary>
        /// Start a metrics session with a new guid
        /// </summary>
        /// <returns>the generated guid</returns>
        public async Task<(string, Error)> StartSession()
        {
            var guid = Guid.NewGuid().ToString();

            Error error = await StartSession(
                guid
            );

            return (guid, error);
        }

        /// <summary>
        /// Start a metrics session with a given unique id.
        /// Uses the Current Users Enabled and Subscribed mods
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <seealso cref="EndSession"/>
        /// <remarks>The id should be completely unique and should not be reused between sessions.</remarks>
        public async Task<Error> StartSession(string id)
        {
            return await StartSession(
                id,
                User.Current.ModRepository.GetSubscribed()
                    .Where(mod => mod.IsSubscribed && mod.IsEnabled)
                    .Select(mod => (long)mod.Id)
                    .ToArray()
            );
        }

        /// <summary>
        /// Start a metrics session with a new guid
        /// Automatically begins heartbeat lifecycle.
        /// </summary>
        /// <param name="mods">The mods to be tracked</param>
        /// <seealso cref="EndSession"/>
        public async Task<(string, Error)> StartSession(long[] mods)
        {
            var guid = Guid.NewGuid().ToString();

            Error error = await StartSession(
                guid,
                mods
            );

            return (guid, error);
        }

        /// <summary>
        /// Start a metrics session with a given unique id.
        /// Automatically begins heartbeat lifecycle.
        /// </summary>
        /// <param name="id">The unique id</param>
        /// <param name="mods">The mods to be tracked</param>
        /// <seealso cref="EndSession"/>
        /// <remarks>The id should be completely unique and should not be reused between sessions.</remarks>
        public async Task<Error> StartSession(string id, long[] mods)
        {
            if (string.IsNullOrEmpty(Secret)) 
                return new Error(ErrorCode.INVALID_METRICS_SECRET);

            if (_sessions.ContainsKey(id))
            {
                ModioLog.Warning?.Log(
                    $"Metric session '{id}' already active in session cache\n" +
                    $"Please start a session with a different ID"
                );
                return Error.None;
            }

            var session = new MetricsSession(id, mods);

            (Error error, Response204? _) =
                await ModioAPI.Metrics.MetricsSessionStart(session.ToRequest(true, Secret));

            if (error)
            {
                ModioLog.Warning?.Log($"Metrics failed with: {error}");
                return error;
            }

            session.Active = true;
            _sessions.Add(session.SessionId, session);

            session.HeartbeatCancellationToken = new CancellationTokenSource();
            
            Heartbeat(session.SessionId).ForgetTaskSafely();
            return Error.None;
        }

        async Task Heartbeat(string id)
        {
            if (string.IsNullOrEmpty(Secret)) return;
            
            if (!_sessions.TryGetValue(id, out MetricsSession session)) return;

            CancellationToken cancellationToken = session.HeartbeatCancellationToken.Token;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    session.SessionOrderId++;

                    (Error error, Response204? _) =
                        await ModioAPI.Metrics.MetricsSessionHeartbeat(session.ToRequest(false, Secret));

                    if (error) ModioLog.Warning?.Log($"Metrics failed with: {error}");

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(HEARTBEAT_INTERVAL), cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                //Always complete the heartbeat
                session.HeartbeatCompletionSource.SetResult(true);
            }
        }
        
        /// <summary>
        /// Ends the session with the given ID.
        /// Will await for the heartbeat task to be cancelled.
        /// </summary>
        /// <param name="id">The unique session ID</param>
        public async Task<Error> EndSession(string id)
        {
            if (string.IsNullOrEmpty(Secret)) return new Error(ErrorCode.INVALID_METRICS_SECRET);
            
            if (!_sessions.TryGetValue(id, out MetricsSession session))
            {
                ModioLog.Warning?.Log(
                    $"Metric session '{id}' not in session cache\n" +
                    $"Please make sure to start a session."
                );

                return Error.None;
            }

            if (!session.HeartbeatCancellationToken.Token.IsCancellationRequested)
                session.HeartbeatCancellationToken.Cancel();

            await session.HeartbeatCompletionSource.Task;
            
            (Error error, Response204? _) =
                await ModioAPI.Metrics.MetricsSessionEnd(session.ToRequest(false, Secret));

            session.Active = false;

            if (error) return Error.None;
            
            ModioLog.Warning?.Log($"Metrics failed with: {error}");
            return error;
        }

        /// <summary>
        /// Ends all sessions in the cache.
        /// </summary>
        public void EndAllSessions()
        {
            foreach (MetricsSession session in _sessions.Values.Where(session => session.Active)) EndSession(session.SessionId).ForgetTaskSafely();
        }
    }
}
