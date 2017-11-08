using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Starcounter;
using Starcounter.Logging;

namespace Joozek78.Star.AsyncHandlers.Internal
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
            Session.ScheduleTask(SessionId,
                (Session session, string sessionId) => {
                    if (session == null)
                    {
                        return;
                    }
                    if (FlushJobs())
                    {
                        session.CalculatePatchAndPushOnWebSocket();
                    }
                });
        }

        /// <summary>
        /// Execute any outstanding jobs scheduled on this context. Call it to prevent more session.CalculatePatchAndPushOnWebSocket() than it is required.
        /// You can safely call it in 
        /// </summary>
        public bool FlushJobs()
        {
            bool flushed = false;
            while (Jobs.TryDequeue(out var jobToRun))
            {
                try
                {
                    jobToRun.Complete();
                }
                catch (Exception e)
                {
                    HandleException(e);
                }
                flushed = true;
            }
            return flushed;
        }

        private static void HandleException(Exception e) => LogSource.LogException(e, "Unhandled exception in async handler");

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