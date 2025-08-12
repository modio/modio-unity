using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modio.Wss.Messages;

namespace Modio.Platforms.Wss
{
    public class WssMessageDispatcher
    {
        public Dictionary<string, WssMessage> UnhandledMessages { get; } = new Dictionary<string, WssMessage>();
        Dictionary<string, TaskCompletionSource<WssMessage>> WaitingMessages { get; } = new Dictionary<string, TaskCompletionSource<WssMessage>>();

        internal void CancelAllAwaitingMessages()
        {
            List<TaskCompletionSource<WssMessage>> awaiters = WaitingMessages.Values.ToList();
            WaitingMessages.Clear();
            foreach(TaskCompletionSource<WssMessage> tcs in awaiters)
                tcs.SetResult(default(WssMessage));
        }
        
        internal async Task<TaskCompletionSource<WssMessage>> WaitForMessages(string messageOperation, bool checkPreviousUnhandledMessages)
        {
            if(checkPreviousUnhandledMessages)
                if (TryGetUnhandledMessage(messageOperation, out WssMessage unhandledMessage))
                {
                    var complete = new TaskCompletionSource<WssMessage>();
                    complete.SetResult(unhandledMessage);
                    return complete;
                }

            var tcs = new TaskCompletionSource<WssMessage>();
            while(WaitingMessages.ContainsKey(messageOperation))
                await WaitingMessages[messageOperation].Task;

            WaitingMessages.Add(messageOperation, tcs);
            return tcs;
        }

        internal bool TryHandleMessage(WssMessage message)
        {
            if (!WaitingMessages.TryGetValue(message.operation, out TaskCompletionSource<WssMessage> action))
                return false;

            WaitingMessages.Remove(message.operation);
            action.SetResult(message);
            
            return true;
        }

        bool TryGetUnhandledMessage(string operation, out WssMessage message)
        {
            if(UnhandledMessages.TryGetValue(operation, out WssMessage unhandledMessage))
            {
                message = unhandledMessage;
                UnhandledMessages.Remove(operation);
                return true;
            }
            message = default(WssMessage);
            return false;

        }

        internal void AddUnhandledMessage(WssMessage message)
        {
            ModioLog.Verbose?.Log(
                $"[Socket] Received unexpected message "
                + $"operation ({message.operation}).\nCaching it "
                + $"temporarily in case we listen for it immediately after."
            );

            UnhandledMessages[message.operation] = message;
        }
    }
}
