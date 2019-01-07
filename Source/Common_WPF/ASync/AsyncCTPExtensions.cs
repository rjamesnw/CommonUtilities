#if !(V1_1 || V2 || V3 || V3_5)

using System;
using System.Reflection;
using System.Threading.Tasks;

#if SILVERLIGHT
// Async on NuGet required for Silverlight: https://www.nuget.org/packages/Microsoft.Bcl.Async
using System.ServiceModel.DomainServices.Client;
#endif

namespace Common
{
    public static class AsyncCTPExtensions
    {
#if SILVERLIGHT
   
        /// <summary>
        /// Run the operation as a task in order to wait on it.
        /// </summary>
        public static Task<T> AsTask<T>(this T operation, bool ignoreErrors = false) where T : OperationBase
        {
            // ... create and return a "watching" task, which will be notified of the result later when ready ...

            var tcs = new TaskCompletionSource<T>(operation.UserState);

            // ... hook into the operation's completed event to get notified when then operation is completed (with or without errors).

            operation.Completed += (sender, e) =>
            {
                if (operation.HasError && !operation.IsErrorHandled)
                {
                    if (!ignoreErrors)
                        tcs.TrySetException(operation.Error);
                    else
                        tcs.TrySetResult(operation);
                    operation.MarkErrorAsHandled();
                }
                else if (operation.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(operation);
                }
            };

            // ... return the "watching" task instance ...

            return tcs.Task;
        }

#endif

        /// <summary>
        /// Wraps events with a task to support async calls with the 'await' statement.
        /// </summary>
        public static Task<TArgs> AsTask<TArgs>(this object obj, string eventName) where TArgs : EventArgs
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (string.IsNullOrWhiteSpace(eventName)) throw new ArgumentNullException("eventName");

            // ... get the event reference for the object ...

            var eventMember = obj.GetType().GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (eventMember == null) throw new MissingMemberException("AsTask(): The event '" + eventName + "' does not exist, or is not accessible.");

            // ... create and return a "watching" task, which will be notified of the result later when ready ...

            var tcs = new TaskCompletionSource<TArgs>(obj);

            // ... hook into the event, then remove when event completes ...

            EventHandler<TArgs> handler = null;
            handler = (sender, e) => { eventMember.RemoveEventHandler(obj, handler); tcs.TrySetResult(e); };
            eventMember.AddEventHandler(obj, handler); // (warning: adding this handler may trigger it immediately in some cases)

            // ... return the "watching" task instance ...

            return tcs.Task;
        }
    }
}

#endif