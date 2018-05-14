using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Starcounter;
using Starcounter.Logging;

namespace Joozek78.Star.Async.Internal
{
    /// <summary>
    /// This synchronization context preserves information about current Starcounter session
    /// </summary>
    public class StarcounterSessionSynchronizationContext : SynchronizationContext
    {
        private readonly string SessionId;

        private readonly ConcurrentQueue<Job> Jobs = new ConcurrentQueue<Job>();
        private static readonly LogSource LogSource = new LogSource(typeof(StarcounterSessionSynchronizationContext).FullName);

        public StarcounterSessionSynchronizationContext()
        {
            SessionId = Session.Current.SessionId;
        }

#pragma warning disable CS0618 // Type or member is obsolete - Session.ScheduleTask
        public override void Send(SendOrPostCallback d, object state)
        {
            Session.ScheduleTask(SessionId,
                (Session session, string sessionId) => {
                    if (session != null)
                    {
                        d(state);
                    }
                }, waitForCompletion: true);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            Jobs.Enqueue(new Job
            {
                Callback = d,
                CallbackArg = state,
                Completed = false
            });
            Session.ScheduleTask(SessionId, (session, sessionId) => {
                if (session == null)
                {
                    return;
                }
                try
                {
                    if (FlushJobs())
                    {
                        session.CalculatePatchAndPushOnWebSocket();
                    }
                }
                catch (Exception e)
                {
                    LogSource.LogException(e, "Unhandled exception in async handler");
                    // behavior copied from Starcounter
                    ((WebSocket)session.ActiveWebSocket).Disconnect(e.ToString().Substring(0, 120), WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                }
            });
        }
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// Execute any outstanding jobs scheduled on this context. Call it to prevent more session.CalculatePatchAndPushOnWebSocket() than it is required.
        /// Call it only in session specific to this context
        /// </summary>
        private bool FlushJobs()
        {
            bool flushed = false;
            while (Jobs.TryDequeue(out var jobToRun))
            {
                jobToRun.Complete();
                flushed = true;
            }
            return flushed;
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        private class Job
        {
            public SendOrPostCallback Callback;
            public object CallbackArg;
            public bool Completed;

            public void Complete()
            {
                if (Completed)
                {
                    return;
                }
                Callback(CallbackArg);
                Completed = true;
            }
        }
    }
}