using System;
using System.Collections.Concurrent;
using System.Threading;
using Starcounter;
using Starcounter.Logging;

namespace Joozek78.Star.AsyncHandlers.Internal
{
    public class StarcounterSynchronizationContext : SynchronizationContext
    {
        private readonly string SessionId;

        private readonly ConcurrentQueue<Job> Jobs = new ConcurrentQueue<Job>();
        private static readonly LogSource LogSource = new LogSource(typeof(StarcounterSynchronizationContext).FullName);

        public StarcounterSynchronizationContext()
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
        /// <returns>true if any jobs were executed, false if none were scheduled</returns>
        public bool FlushJobs()
        {
            if (Current != this)
            {
                throw new InvalidOperationException($"This method can be called only on current SynchronizationContext");
            }
            bool flushed = false;
            while (Jobs.TryDequeue(out var jobToRun))
            {
                try
                {
                    jobToRun.Complete();
                }
                catch (Exception e)
                {
                    LogSource.LogException(e, "Unhandled exception in async handler");
                }
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