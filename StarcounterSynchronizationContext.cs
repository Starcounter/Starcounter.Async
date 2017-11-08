using System.Threading;
using Starcounter;

namespace StarcounterApplication4
{
    public class StarcounterSynchronizationContext : SynchronizationContext
    {
        private readonly string SessionId;

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
                }, true);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            Session.ScheduleTask(SessionId,
                (Session session, string sessionId) => {
                    if (session != null)
                    {
                        d(state);
                        session.CalculatePatchAndPushOnWebSocket();
                    }
                });
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }
}