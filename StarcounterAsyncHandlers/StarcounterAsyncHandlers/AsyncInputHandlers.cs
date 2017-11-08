using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Joozek78.Star.AsyncHandlers.Internal;
using Nito.AsyncEx;
using Starcounter;
using Starcounter.Templates;

namespace Joozek78.Star.AsyncHandlers
{
    public class AsyncInputHandlers
    {
        /// <summary>
        /// Enables usage of async in Input Handler by temporarily switching <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <para>This method will execute the supplied action once, in synchronization context that is aware of Starcoutner sesssion</para>
        /// <example><code>
        /// public void Handle(Input.StartWorkTrigger input)
        /// {
        ///   AsyncInputHandlers.RunAsync(async () => {
        ///     ShowBusyIndicator = true;
        ///     await Task.Run(BackgroundTaskAsync);
        ///     ShowBusyIndicator = false;
        ///   });
        /// }
        /// </code></example>
        /// <remarks>Uncaught exceptions will be logged to Starcounter log</remarks>
        /// <param name="asyncAction">will be executed exactly once. It is intended to be an async void lambda or method</param>
        public static void Run(Action asyncAction)
        {
            using (new SynchronizationContextSwitcher(new StarcounterSessionSynchronizationContext()))
            {
                asyncAction();
            }
        }
    }
}