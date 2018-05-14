using System;
using System.Threading;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Startup.Routing;

namespace Joozek78.Star.Async.Examples
{
    [Url("/AsyncHandlers/Background")]
    partial class BackgroundWorkPage : Json
    {
        private CancellationTokenSource WorkCancellationTokenSource;
        public void Handle(Input.StartSimpleWorkTrigger input)
        {
            AsyncInputHandlers.Run(async () => {
                WorkProgress = "Started";
                await Task.Delay(TimeSpan.FromSeconds(1));
                WorkProgress = "Done";
            });
        }

        public void Handle(Input.StartFaultyWorkTrigger input)
        {
            AsyncInputHandlers.Run(async () => {
                IProgress<int> progress = new Progress<int>(i => WorkProgress = i.ToString());
                try
                {
                    await Task.Run(async () => await FaultyJob(progress));
                }
                catch (Exception e)
                {
                    WorkProgress += $" (error caught in the UI thread: {e.Message})";
                }
            });
        }

        private static async Task FaultyJob(IProgress<int> progress)
        {
            for (int i = 1; i <= 13; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                progress.Report(i);
            }
            throw new Exception("thrown in a thread pool");
        }

        public void Handle(Input.StartProgressWorkTrigger input)
        {
            AsyncInputHandlers.Run(async () => {
                IProgress<int> progress = new Progress<int>(i => WorkProgress = i.ToString());
                await Task.Run(async () => {
                    for (int i = 1; i <= 30; i++)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                        progress.Report(i);
                    }
                });
            });
        }


        public void Handle(Input.StartCancellableWorkTrigger input)
        {
            AsyncInputHandlers.Run(async () => {
                WorkCancellationTokenSource?.Cancel();
                WorkCancellationTokenSource = new CancellationTokenSource();
                var cts = WorkCancellationTokenSource;
                IProgress<int> progress = new Progress<int>(i => WorkProgress = i.ToString());
                try
                {
                    await Task.Run(async () => {
                            for (int i = 1; i <= 100; i++)
                            {
                            // if we referenced the WorkCancellationTokenSource field directly, we would have a race condition between
                            // background and scheduler (UI) threads
                            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
                                progress.Report(i);
                            }
                        },
                        cts.Token);
                }
                catch (TaskCanceledException)
                {
                    WorkProgress += " (Canceled)";
                }
            });
        }

        public void Handle(Input.CancelWorkTrigger input)
        {
            WorkCancellationTokenSource?.Cancel();
        }
    }
}
