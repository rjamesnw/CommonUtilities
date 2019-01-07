using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Common;
using System.ServiceModel;

namespace Common.WPF.Scripting
{
    /// <summary>
    /// Provides a means to execute code in stages, to further support asynchronous server method calls.
    /// The code is created in sections AROUND the server call(s) using Lambda statement blocks.
    /// This allows uninterrupted access to local-scoped and class-scoped variables, as well as method
    /// parameters they are used in.
    /// Each code block, in the order added, is dispatched using the WPF "Dispatch" queuing system.
    /// This also has the benefit of allowing cancellations of long operations by checking the UI between
    /// each code block.
    /// </summary>
    public partial class CodeBlockQueueScript
    {
        // -------------------------------------------------------------------------------------------------
        public enum States { Stopped, Running, Paused, ServiceCallWait, LoopDelay };
        public enum CodeBlockType { Action, Function, ServiceCall, ServiceCallErrorHandler };
        // -------------------------------------------------------------------------------------------------

        class __Dispatcher : DependencyObject { }
        __Dispatcher _Dispatcher = new __Dispatcher();

        // -------------------------------------------------------------------------------------------------

        // (note: like timeline channels)
        class _CodeBlock { public string Label; public Delegate Method; public CodeBlockType MethodType; }
        readonly List<_CodeBlock> _CodeList = new List<_CodeBlock>(3);
        string _Label = ""; // (temporarily set when using the this[] indexer)
        int _InvokationIndex = -1;

        object _Result = null;
        object _ASyncCallUserData = null;

        public bool Stepping = false; // (if stepping is true, then code block execution does not automatically advance)
        bool _Paused = false; // (call Continue() to continue/step to next code block)
        public bool Looped = false; // (whether or not to loop back to the beginning continuously)
        public uint LoopDelay = 0; // (the delay, in ms, before looping back)

        DispatcherTimer _Timer; // (use if loops are enabled)

        public States State
        {
            get { return _State; }
            private set
            {
                if ((value == States.Running || value == States.Stopped) && _Paused)
                    _State = States.Paused;
                else
                    _State = value;
            }
        }
        States _State = States.Stopped;

        // -------------------------------------------------------------------------------------------------

        public T GetResultAs<T>() { return (T)_Result; }

        public int CodeBlockQueueLength { get { return _CodeList.Count; } }

        // -------------------------------------------------------------------------------------------------

        public CodeBlockQueueScript()
        {
        }

        // -------------------------------------------------------------------------------------------------

        void _CheckIfLabelExists()
        {
            if (_GetCodeBlockIndex(_Label) >= 0)
                throw new InvalidOperationException("Script: Label '" + _Label + "' was already used.");
        }

        // -------------------------------------------------------------------------------------------------

        private CodeBlockQueueScript _Add(Delegate method, CodeBlockType type)
        {
            _CheckIfLabelExists();
            if (type != CodeBlockType.ServiceCallErrorHandler)
                _AddServiceCallCheckIfMissing();
            var d = new _CodeBlock { Label = _Label, Method = method, MethodType = type };
            _CodeList.Add(d);
            if (!string.IsNullOrEmpty(_Label)) _Label = "";
            return this;
        }

        public static CodeBlockQueueScript operator +(CodeBlockQueueScript subject, Action action)
        {
            return subject._Add(action, CodeBlockType.Action);
        }

        public static CodeBlockQueueScript operator +(CodeBlockQueueScript subject, Action<object> action)
        {
            return subject._Add(action, CodeBlockType.Action);
        }
        public static CodeBlockQueueScript operator +(CodeBlockQueueScript subject, Action<object, object> action)
        {
            return subject._Add(action, CodeBlockType.Action);
        }
        public static CodeBlockQueueScript operator +(CodeBlockQueueScript subject, Func<object> func)
        {
            return subject._Add(func, CodeBlockType.Function);
        }
        public static CodeBlockQueueScript operator +(CodeBlockQueueScript subject, Func<object, object> func)
        {
            return subject._Add(func, CodeBlockType.Function);
        }
        public static CodeBlockQueueScript operator +(CodeBlockQueueScript subject, Func<object, object, object> func)
        {
            return subject._Add(func, CodeBlockType.Function);
        }

        // -------------------------------------------------------------------------------------------------

        object[] _CurrentAsyncCallInfo;

        /// <summary>
        /// Adds an asynchronous service method call to the script queue.
        /// <para>Optionally, there may be AddAsyncCall_?() extension methods that can be used for convenience where available.</para>
        /// </summary>
        /// <param name="commObject">The client service communication object.</param>
        /// <param name="serviceMethodName">The server method name.</param>
        /// <param name="userData">Any user-specific data to keep track of.</param>
        /// <param name="preCallAction">A call-back action to execute just before the call is made.</param>
        /// <param name="postCallAction">A call-back action to execute just after a response is received (for any reason).</param>
        /// <param name="args">Arguments expected by the service method.</param>
        public void AddAsyncCall(ICommunicationObject commObject, string serviceMethodName, object userData, Action preCallAction, Action postCallAction, params object[] args)
        {
            if (commObject == null)
                throw new ArgumentNullException("commObject");

            var asyncMethodName = serviceMethodName + "Async";
            // ... the async methods are overloaded (once accepts an extra "UserState"), so select the on with the greatest # of params ...
            var overLoads = from m in commObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                            where m.Name == asyncMethodName
                            select new { Method = m, ParamCount = m.GetParameters().Count() };
            if (overLoads.Count() == 0)
                throw new MissingMethodException("Method '" + serviceMethodName + "Async' not found on object '" + commObject.GetType().FullName + "'.");
            MethodInfo mi = overLoads.First().Method;
            foreach (var mDetails in overLoads)
                if (mDetails.ParamCount > mi.GetParameters().Count())
                    mi = mDetails.Method;

            EventInfo ei = commObject.GetType().GetEvent(serviceMethodName + "Completed", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (ei == null)
                throw new MissingMethodException("Event '" + serviceMethodName + "Completed' not found on object '" + commObject.GetType().FullName + "'.");

            var eventDelegate = Delegate.CreateDelegate(ei.EventHandlerType, this, this.GetType().GetMethod("_ASyncCompleted"));
            //??ei.RemoveEventHandler(commObject, eventDelegate); // (prevents from being added twice)
            ei.AddEventHandler(commObject, eventDelegate);

            var asyncCallParams = new object[args.Length + 1];
            Array.Copy(args, asyncCallParams, args.Length);
            var callInfo = new object[] { this, commObject, ei, eventDelegate, userData, postCallAction };
            asyncCallParams[asyncCallParams.Length - 1] = callInfo;

            _Add((Action)(() =>
                {
                    State = States.ServiceCallWait; // (wait for following invoke to complete - will continue automatically)
                    _CurrentAsyncCallInfo = callInfo;
                    if (preCallAction != null) preCallAction();
                    mi.Invoke(commObject, asyncCallParams); // Note: Last argument of '_args' is a user-state parameter
                })
                , CodeBlockType.ServiceCall);

        }

        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// A static event that is called for every completed service call.
        /// This event can be used to intercept all calls to detect, for example,
        /// a method error result from the service call (i.e. custom error checks).
        /// </summary>
        public static event Action<CodeBlockQueueScript> ServiceCallCompleted;

        /// <summary>
        /// Adds the specified delegate to the code block list and only executes it if a service call fails.
        /// Each time the property is set, a new entry is created (the previous setting is NOT replaced).
        /// As well, this should be set directly after a service call request is added.
        /// If you will not handle any errors yourself, then be sure to call 'HandleServiceCallErrors()'
        /// at the end of the code block (note: may throw an exception, so make sure it's at the end).
        /// </summary>
        public Action<AsyncCompletedEventArgs> ServiceCallFailed
        {
            set
            {
                _Add((Action)(() =>
                    {
                        if (_LastAsyncCompletedEventArgs != null)
                        {
                            if (_LastAsyncCompletedEventArgs.Cancelled || _LastAsyncCompletedEventArgs.Error != null)
                                value(_LastAsyncCompletedEventArgs);
                            _LastAsyncCompletedEventArgs = null;
                        }
                    })
                    , CodeBlockType.ServiceCallErrorHandler);
            }
        }

        void _AddServiceCallCheckIfMissing()
        {
            var codeBlock = _GetLastCodeBlock();
            if (codeBlock != null && codeBlock.MethodType == CodeBlockType.ServiceCall)
                ServiceCallFailed = (args) => { GoToEnd(); HandleServiceCallErrors(); };
        }

        /// <summary>
        /// See the "ServiceCallFailed" property.
        /// </summary>
        public void HandleServiceCallErrors()
        {
            if (_LastAsyncCompletedEventArgs != null && _LastAsyncCompletedEventArgs.Error != null)
                throw _LastAsyncCompletedEventArgs.Error;
        }

        // -------------------------------------------------------------------------------------------------

        AsyncCompletedEventArgs _LastAsyncCompletedEventArgs;

        public void _ASyncCompleted(object subject, AsyncCompletedEventArgs e)
        {
            // ... verify user state object type and that it belongs to this script instance ...

            if (e.UserState == _CurrentAsyncCallInfo)
            {
                try
                {
                    _LastAsyncCompletedEventArgs = e;

                    object[] @params = e.UserState as object[];

                    var commObject = (ICommunicationObject)@params[1];
                    var ei = (EventInfo)@params[2];
                    Delegate eventDelegate = (Delegate)@params[3];
                    ei.RemoveEventHandler(commObject, eventDelegate); // (prevents from being called again)
                    _ASyncCallUserData = @params[4];

                    if (@params[5] != null) ((Action)@params[5])(); // (post call-back action, if specified)

                    _Result = e.Error != null ? null : Objects.GetPropertyOrFieldValue<object>(e, "Result"); // TODO: "Result" is too CPP specific...need to find a better way.

                    if (ServiceCallCompleted != null)
                        try { ServiceCallCompleted(this); }
                        catch (Exception ex) { _LastAsyncCompletedEventArgs = new AsyncCompletedEventArgs(ex, e.Cancelled, e.UserState); }

                    State = States.Running;
                    if (!Stepping)
                        _DoNext();
                }
                finally
                {
                    _CurrentAsyncCallInfo = null;
                }
            }
        }

        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Cancel the last service call and, optionally, end the script.
        /// Note: Ending a looped script doesn't stop the loop, but only jumps to the end.
        /// </summary>
        /// <param name="endScript"></param>
        public void CancelServiceCall(bool endScript)
        {
            if (_CurrentAsyncCallInfo != null)
            {
                //??var commObject = (ICommunicationObject)_AsyncCallParams[1];
                //??commObject.BeginClose(null, _AsyncCallParams);
                _CurrentAsyncCallInfo = null;
                if (endScript)
                    GoToEnd();
                else
                {
                    State = States.Running;
                    _DoNextAsync();
                }
            }
        }

        // -------------------------------------------------------------------------------------------------

        void _DoNextAsync() { _Dispatcher.Dispatcher.BeginInvoke((Action)_DoNext); }

        void _DoNext()
        {
            if (_InvokationIndex >= _CodeList.Count)
                throw new IndexOutOfRangeException("Script: No more code blocks left to invoke.");
            if (_InvokationIndex < 0)
                throw new IndexOutOfRangeException("Script: Invocation index must be >= 0.");

            if (State == States.Paused && !_Paused)
                State = States.Running;

            if (State == States.Running)
            {
                var methodDetails = _CodeList[_InvokationIndex++];
                var method = methodDetails.Method;

                // (make sure we end with a service call error check, if not yet specified)
                // (note: this handles cases where a specialized AddAsyncCall_?() extension method is added as the last code block)
                if (methodDetails.MethodType == CodeBlockType.ServiceCall && _InvokationIndex == _CodeList.Count)
                    _AddServiceCallCheckIfMissing(); // TODO: Is this best done here?

                try
                {
                    bool wasAction = false;
                    if (method is Action)
                    { ((Action)method)(); wasAction = true; }
                    else if (method is Action<object>)
                    { ((Action<object>)method)(_Result); wasAction = true; }
                    else if (method is Action<object, object>)
                    { ((Action<object, object>)method)(_Result, _ASyncCallUserData); wasAction = true; }
                    else if (method is Func<object>)
                    { _Result = ((Func<object>)method)(); }
                    else if (method is Func<object, object>)
                    { _Result = ((Func<object, object>)method)(_Result); }
                    else if (method is Func<object, object, object>)
                    { _Result = ((Func<object, object, object>)method)(_Result, _ASyncCallUserData); }

                    if (wasAction && methodDetails.MethodType != CodeBlockType.ServiceCallErrorHandler)
                        _Result = null;
                }
                finally
                {
                    if (_InvokationIndex < _CodeList.Count)
                    {
                        if (State == States.Running)
                        {
                            if (_Paused) State = States.Paused;
                            if (!Stepping) _DoNextAsync();
                        }
                    }
                    else
                    {
                        State = States.Stopped;
                        if (Looped)
                            _DoLoop();
                    }
                }
            }
        }

        void _DoLoop()
        {
            if (Looped)
            {
                State = States.Running;
                _InvokationIndex = 0;
                if (LoopDelay == 0)
                    _DoNextAsync();
                else
                {
                    State = States.LoopDelay;
                    _Timer = new DispatcherTimer();
                    _Timer.Interval = new TimeSpan(0, 0, 0, 0, (int)LoopDelay);
                    _Timer.Tick += new EventHandler(_Timer_Tick);
                    _Timer.Start();
                }
            }
        }

        void _TerminateLoopDelay()
        {
            if (_Timer != null)
            {
                _Timer.Stop();
                _Timer.Tick -= _Timer_Tick;
                _Timer = null;
                State = States.Stopped;
            }
        }

        void _Timer_Tick(object sender, EventArgs e)
        {
            _TerminateLoopDelay();
            State = States.Running;
            _DoNext();
        }

        // -------------------------------------------------------------------------------------------------

        int _GetCodeBlockIndex(string label)
        {
            if (!string.IsNullOrEmpty(label))
                for (int i = 0; i < _CodeList.Count; i++)
                    if (_CodeList[i].Label == label)
                        return i;
            return -1;
        }

        _CodeBlock _GetLastCodeBlock()
        {
            if (_CodeList.Count > 0)
                return _CodeList[_CodeList.Count - 1];
            return null;
        }

        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Jump to a specific code block.
        /// </summary>
        /// <param name="label">Code block label.</param>
        public void GoTo(string label)
        {
            int i = _GetCodeBlockIndex(label);
            if (i == -1)
                throw new InvalidOperationException("Script: Label '" + label + "' does not exist.");
            _InvokationIndex = i; // (queue requested code block next)
        }

        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Jump to a specific code block and execute immediately.
        /// </summary>
        /// <param name="label">Code block label.</param>
        public void GoNow(string label)
        {
            GoTo(label);
            _DoNext();
        }

        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Jump to the end of the code blocks. If a loop is being used, then after "LoopDelay" has
        /// passed, the script starts over again, otherwise the script effectively ends.
        /// </summary>
        /// <param name="label">Code block label.</param>
        public void GoToEnd()
        {
            if (_InvokationIndex < _CodeList.Count)
            {
                _InvokationIndex = _CodeList.Count;
                State = States.Stopped;
                _DoLoop();
            }
        }

        // -------------------------------------------------------------------------------------------------

        public CodeBlockQueueScript this[string label]
        {
            get { _Label = label; return this; }
            set { _Label = label; if (value != this) throw new InvalidOperationException("Script[\"" + label + "\"]: Indexer provided for use with += operations, and cannot be set with another instance."); }
        }

        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts the code block execution sequence.
        /// </summary>
        public void Start()
        {
            if (_InvokationIndex >= _CodeList.Count && !Looped)
                throw new InvalidOperationException("Script.Start(): Script has ended.");
            if (_InvokationIndex >= 0)
                throw new InvalidOperationException("Script.Start(): Already started.");
            _InvokationIndex = 0;
            State = States.Running;
            _DoNext();
        }

        public void Pause()
        {
            if (State != States.Stopped && !_Paused)
            {
                _Paused = true;
                State = State;
            }
        }

        /// <summary>
        /// If a code block is paused, or loop-delayed, this causes it to continue.
        /// </summary>
        /// <returns></returns>
        public bool Continue()
        {
            if (_InvokationIndex < 0)
                throw new InvalidOperationException("Script.Continue(): Execution not started yet - call Start() first.");
            if (_InvokationIndex >= _CodeList.Count)
                throw new InvalidOperationException("Script.Continue(): No more code blocks left to run.");

            _Paused = false;

            if (_Timer != null)
                _Timer_Tick(_Timer, null);
            else
                _DoNextAsync();

            return (_InvokationIndex < _CodeList.Count);
        }

        // -------------------------------------------------------------------------------------------------
    }
}
