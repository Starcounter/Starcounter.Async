using System;
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
        /// <remarks>This method should be called before any await invocations and its return value must be disposed before input handler returns</remarks>
        /// <example><code>
        /// public void Handle(Input.StartWorkTrigger input)
        /// {
        ///   using(AsyncHandlers.Enable())
        ///   {
        ///     ShowBusyIndicator = true;
        ///     await Task.Run(BackgroundTaskAsync);
        ///     ShowBusyIndicator = false;
        ///   }
        /// }
        /// </code></example>
        /// <returns>When disposed, the <see cref="SynchronizationContext"/> will be switched back to the original</returns>
        public static IDisposable Enable() => new SynchronizationContextSwitcher(new StarcounterSynchronizationContext());

        /// <inheritdoc cref="ChangeOfProperty{TClr,TJson}"/>
        public static Task<string> ChangeOfProperty(TString property) => ChangeOfProperty<string, TString>(property);

        /// <inheritdoc cref="ChangeOfProperty{TClr,TJson}"/>
        public static Task<long> ChangeOfProperty(TLong property) => ChangeOfProperty<long, TLong>(property);

        /// <inheritdoc cref="ChangeOfProperty{TClr,TJson}"/>
        public static Task<bool> ChangeOfProperty(TBool property) => ChangeOfProperty<bool, TBool>(property);
        
        /// <summary>
        /// Returns a task which will complete when <paramref name="property"/> is changed.
        /// </summary>
        /// <param name="property">Can be obtained from Template property of Typed JSON page.</param>
        /// <remarks>The existing input handler of <paramref name="property"/> will be overwritten. It means that the following code will not work:
        /// <code>
        /// public void Handle(Input.DeleteTrigger input)
        /// {
        ///   using(AsyncHandlers.Enable())
        ///   {
        ///     Question = "Are you sure?";
        ///     await AsyncHandlers.ChangeOfProperty(ConfirmTrigger);
        ///     ProceedDelete();
        ///   }
        /// }
        /// 
        /// public void Handle(Input.ConfirmTrigger input)
        /// {
        ///   // after calling AsyncHandlers.ChangeOfProperty(ConfirmTrigger)
        ///   // this input handler will no longer work
        /// }
        /// </code></remarks>
        /// <returns>Task that will be completed when the given property receives a change patch. This task will never be faulted or canceled.</returns>
        private static Task<TClr> ChangeOfProperty<TClr, TJson>(TJson property) where TJson : Property<TClr>
        {
            var starcounterSynchronizationContext = SynchronizationContext.Current as StarcounterSynchronizationContext
                                                    ?? throw new InvalidOperationException($"{nameof(ChangeOfProperty)} can only be called if current synchronization context is {nameof(StarcounterSynchronizationContext)}");
            var taskCompletionSource = new TaskCompletionSource<TClr>();
            property.AddHandler((page, prop, value) => new SimpleInput<TClr>() { Value = value },
                (page, input) => {
                    taskCompletionSource.TrySetResult(input.Value);
                    starcounterSynchronizationContext.FlushJobs();
                });
            return taskCompletionSource.Task;
        }

        private class SimpleInput<T> : Input<T> { }
    }
}