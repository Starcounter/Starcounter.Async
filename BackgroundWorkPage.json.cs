using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Starcounter;
using Starcounter.Authorization.Routing;
using Starcounter.Templates;

namespace StarcounterApplication4
{
    [Url("/background")]
    partial class BackgroundWorkPage : Json
    {
        private CancellationTokenSource WorkCancellationTokenSource;
        public async void Handle(Input.StartSimpleWorkTrigger input)
        {
            using (new SynchronizationContextSwitcher(new StarcounterSynchronizationContext()))
            {
                WorkProgress = "Started";
                await Task.Delay(TimeSpan.FromSeconds(1));
                WorkProgress = "Done";
            }
        }

        public async void Handle(Input.StartProgressWorkTrigger input)
        {
            using (new SynchronizationContextSwitcher(new StarcounterSynchronizationContext()))
            {
                IProgress<int> progress = new Progress<int>(i => WorkProgress = i.ToString());
                await Task.Factory.Run(async () =>
                {
                    for (int i = 1; i <= 30; i++)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                        progress.Report(i);
                    }
                });
            }
        }
        public async void Handle(Input.StartCancellableWorkTrigger input)
        {
            using (new SynchronizationContextSwitcher(new StarcounterSynchronizationContext()))
            {
                WorkCancellationTokenSource?.Cancel();
                WorkCancellationTokenSource = new CancellationTokenSource();
                var cts = WorkCancellationTokenSource;
                IProgress<int> progress = new Progress<int>(i => WorkProgress = i.ToString());
                try
                {
                    await Task.Factory.Run(async () => {
                        for (int i = 1; i <= 100; i++)
                        {
                            // if we referenced the field directly, we would have a race condition between
                            // background and scheduler (UI) threads
                            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
                            progress.Report(i);
                        }
                    });
                }
                catch (TaskCanceledException)
                {
                    WorkProgress += " (Canceled)";
                }
            }
        }

        public void Handle(Input.CancelWorkTrigger input)
        {
            WorkCancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Returns a task which will complete when <paramref name="property"/> is changed.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static Task<long> ChangeOfProperty(TLong property)
        {
            var taskCompletionSource = new TaskCompletionSource<long>();
            property.AddHandler((page, prop, value) => new Input.StartCancellableWorkTrigger { App = (BackgroundWorkPage)page, Template = (TLong)prop, Value = value },
                (page, input) => { taskCompletionSource.TrySetResult(input.Value); });
            return taskCompletionSource.Task;
        }

    }
}
