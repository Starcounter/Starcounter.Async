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
    public class AwaitablePropertyHelper
    {
        private readonly Dictionary<Type, object> Things = new Dictionary<Type, object>();
        public Task GetPropertyAwaitable<T>() where T : Input<long>
        {
            return GetPropertyAwaitable<long, T>();
        }

        public Task GetPropertyAwaitableString<T>() where T : Input<string>
        {
            return GetPropertyAwaitable<string, T>();
        }

        public Task<TClr> GetPropertyAwaitable<TClr, TInput>() where TInput : Input<TClr>
        {
            if (Things.ContainsKey(typeof(TInput)))
            {
                throw new InvalidOperationException($"Property {nameof(TInput)} is already being awaited");
            }
            var taskCompletionSource = new TaskCompletionSource<TClr>();
            Things.Add(typeof(TInput), new PropertyEntry<TClr>()
            {
                TaskCompletionSource = taskCompletionSource,
                SynchronizationContext = SynchronizationContext.Current as StarcounterSessionSynchronizationContext
            });
            return taskCompletionSource.Task;
        }

        public void NotifyPropertyChanged<TClr, TInput>(TInput input) where TInput:Input<TClr>
        {
            try
            {
//                var propertyEntry = (PropertyEntry<TClr>) Things[typeof(TInput)];
//                propertyEntry.TaskCompletionSource.TrySetResult(input.Value);
//                propertyEntry.SynchronizationContext?.FlushJobs();
//                Task t;
//                t.
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException($"Property {nameof(TInput)} was never awaited");
            }
        }

        private struct PropertyEntry<T>
        {
            public TaskCompletionSource<T> TaskCompletionSource;
            public StarcounterSessionSynchronizationContext SynchronizationContext;
        }

    }
    public class AsyncInputHandlers
    {
        public static IDisposable Enable() => new SynchronizationContextSwitcher(new StarcounterSessionSynchronizationContext());

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
        /// <param name="asyncAction">will be executed exactly once. It is intended to be an async void lambda or method</param>
        /// <returns>When disposed, the <see cref="SynchronizationContext"/> will be switched back to the original</returns>
        public static void RunAsync(Action asyncAction)
        {
            using (new SynchronizationContextSwitcher(new StarcounterSessionSynchronizationContext()))
            {
                asyncAction();
            }
        }

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
            var starcounterSynchronizationContext = SynchronizationContext.Current as StarcounterSessionSynchronizationContext
                                                    ?? throw new InvalidOperationException($"{nameof(ChangeOfProperty)} can only be called if current synchronization context is {nameof(StarcounterSessionSynchronizationContext)}");
            var taskCompletionSource = new TaskCompletionSource<TClr>();
            property.AddHandler((page, prop, value) => new SimpleInput<TClr>() { Value = value },
                (page, input) => {
                    taskCompletionSource.TrySetResult(input.Value);
                    starcounterSynchronizationContext.FlushJobs();
                });
            return taskCompletionSource.Task;
        }

        private class SimpleInput<T> : Input<T> { }

//        public static void Enable2(Type pageType)
//        {
//            var handleAsyncMethods = pageType
//                .GetMethods(BindingFlags.Public)
//                .Where(methodInfo => methodInfo.Name == "HandleAsync")
//                .Select(methodInfo => (method:methodInfo, @params: methodInfo.GetParameters()))
//                .Where(tuple => tuple.@params.Length == 1)
//                .Select(tuple => (method: tuple.method, param: tuple.@params.First()))
//                .Where(tuple => tuple.param.ParameterType.IsSubclassOf(typeof(Input)))
//                .ToList();
//            var errors = handleAsyncMethods
//                .Where(tuple => tuple.method.ReturnType != typeof(Task))
//                .Select(tuple => $"Method {tuple.method.Name} is invalid: async handlers must return Task")
//                .ToList();
//            if (errors.Any())
//            {
//                throw new InvalidOperationException($"There were errors while enabling async handlers: {string.Join(", ", errors)}");
//            }
////
////            var properties = GetPropertiesOfPage(pageType).ToList();
////            foreach (var asyncHandler in handleAsyncMethods)
////            {
////                var property = FindPropertyByInputType(pageType, properties);
////                property.
////            }
//        }

//        private static IEnumerable<Template> GetPropertiesOfPage(Type pageType)
//        {
//            try
//            {
//                // the name "DefaultTemplate" is defined inside each Page class, so couldn't be obtained with nameof
//                var pageDefaultTemplate = (TObject)pageType.GetField("DefaultTemplate").GetValue(null);
//                return pageDefaultTemplate.Children;
//            }
//            catch (Exception ex)
//            {
//                throw new Exception(
//                    $"Could not access DefaultTemplate of page class {pageType}",
//                    ex);
//            }
//        }
//
//        private static Template FindPropertyByInputType(Type inputType, IEnumerable<Template> candidates)
//        {
//            try
//            {
//                // type Input.CancelTrigger will have 'Name' property 'CancelTrigger'
//                var propertyName = inputType.Name;
//                return candidates.First(template => template.PropertyName == propertyName);
//            }
//            catch (Exception ex)
//            {
//                throw new Exception($"Could not determine property for Input type: {inputType}", ex);
//            }
//        }
    }
}