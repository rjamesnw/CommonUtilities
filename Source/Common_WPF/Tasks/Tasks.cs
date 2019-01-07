using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Common.Tasks
{
    public static class TimedTasks
    {
        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Provided as the controller's "heart beat".
        /// This is used mainly to keep the GUI, and public data objects, updated.
        /// </summary>
        public static readonly DispatcherTimer TaskTimer;

        // --------------------------------------------------------------------------------------------------------

        // (Note: As soon as the controller instance is created, the timer is also created and the tasks begin to run.)
        public static readonly ObservableCollection<TimedTask> Tasks;

        static void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (var item in e.OldItems)
                    ((TimedTask)item)._DoFinally();

            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                    ((TimedTask)item)._LastTickTime = DateTime.Now;
        }

        // --------------------------------------------------------------------------------------------------------

        static TimedTasks()
        {
            TaskTimer = new DispatcherTimer();
            Tasks = new ObservableCollection<TimedTask>();

            if (!WPFUtilities.InDesignMode)
            {
                Tasks.CollectionChanged += Tasks_CollectionChanged;

                TaskTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
                TaskTimer.Tick += _OnTick;
                TaskTimer.Start();
            }
        }

        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Base on the current TaskTimer.Interval setting, this converts seconds into timer "ticks".
        /// </summary>
        public static double SecondsToTicks(double seconds)
        { return (1000d / (double)TaskTimer.Interval.TotalMilliseconds) * Math.Abs(seconds); }

        /// <summary>
        /// Base on the current TaskTimer.Interval setting, this converts milliseconds into timer "ticks".
        /// </summary>
        public static double MillisecondsToTicks(double ms)
        { return Math.Abs(ms) / (double)TaskTimer.Interval.TotalMilliseconds; }

        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Executes the event on an interval defined via TaskTimer.Interval.
        /// This "Tick" event is provided has a "heartbeat" for various public and external tasks.
        /// The default interval is 1 millisecond.
        /// This default can be changed by setting your own value for TaskTimer.Interval (Warning! This effects the WHOLE operating system. Your OWN 'public' should be set when the task is created!).
        /// Warning: The default is 1000 ticks per second, so it is advised to make sure your tasks are QUICK, otherwise you hold up other tasks. If a task will take awhile, best practice is to use Dispatching.Dispatch() to trigger the actual task.
        /// </summary>
        public static event Action Tick;

        static void _OnTick(object sender, EventArgs e)
        {
            // ... run tasks first before executing the tick event ...

            TimedTask task = null;

            for (int i = 0; i < Tasks.Count; i++)
            {
                task = Tasks[i];

                if (task.Paused) continue;

                task.TickCounter += task.TicksSinceLastTick;
                task._LastTickTime = DateTime.Now;

                if (task.TickCounter >= task.TickDelay)
                {
                    task.Run(false); // (note: also resets TickCounter to 0, so set this to false)

                    if (task.TickDelay > 0)
                        task.TickCounter %= task.TickDelay; // (reset the tick counter to the remaining fractional ticks, if any)
                    else
                        task.TickCounter = 0;
                }
            }

            // ... execute tick event ...

            if (Tick != null)
                Tick();
        }

        // ------------------------------------------------------------------------------------------------------------

        public static TimedTask AddTask(int delay, int repeatCount, Action simpleHandler)
        {
            TimedTask task = new TimedTask(delay, repeatCount, _SimpleTaskHandlerProxy, simpleHandler);
            Tasks.Add(task);
            return task;
        }

        public static TimedTask AddTask(int delay, Action simpleHandler)
        {
            TimedTask task = new TimedTask(delay, _SimpleTaskHandlerProxy, simpleHandler);
            Tasks.Add(task);
            return task;
        }

        static void _SimpleTaskHandlerProxy(TimedTask taskInfo, object data) // (a bridge which reads "data" as the simple action to execute)
        { ((Action)data)(); }

        // ------------------------------------------------------------------------------------------------------------

        public static TimedTask AddTask(int delay, TaskHandler taskHandler)
        {
            TimedTask task = new TimedTask(delay, taskHandler);
            Tasks.Add(task);
            return task;
        }

        public static TimedTask AddTask(int delay, TaskHandler taskHandler, object data)
        {
            TimedTask task = new TimedTask(delay, taskHandler, data);
            Tasks.Add(task);
            return task;
        }

        /// <summary>
        /// Add a task to execute at a later time, or a number of times.
        /// Note: If 'tickDelay' is 0, and 'repeatCount' is -1 or less, then the task repeats EVERY TICK
        /// at the speed specified by TaskTimer.Interval.
        /// Application responsiveness can greatly be reduced if care is not taken to make
        /// fast repeating tasks execute quickly.
        /// </summary>
        /// <param name="app">The application that the task belongs to.</param>
        /// <param name="delay">
        /// Number of milliseconds to wait before running the task.
        /// A value of 0 runs the task immediately.
        /// A value less than 0 specifies ticks instead of milliseconds.
        /// As an example, if the system timer resolution is 100, then that means 100 ticks for 1 second (or 1000 ms).
        /// "Ticks" are sometimes used in games, so that the game engine can report delta time elapsed on each frame.
        /// </param>
        /// <param name="repeatCount">Number of times to repeat the task (anything less than 0 is infinite).</param>
        /// <param name="taskHandler">Call-back method, which is the task to run.</param>
        /// <param name="data">A reference to any data needed by the task method.</param>
        public static TimedTask AddTask(int delay, int repeatCount, TaskHandler taskHandler, object data)
        {
            TimedTask task = new TimedTask(delay, repeatCount, taskHandler, data);
            Tasks.Add(task);
            return task;
        }

        // ------------------------------------------------------------------------------------------------------------

        public static void RemoveTask(TimedTask task)
        {
            if (Tasks.Contains(task))
                Tasks.Remove(task);
        }

        // --------------------------------------------------------------------------------------------------------

        public static void RemoveAllTasks()
        {
            for (int i = Tasks.Count - 1; i >= 0; i--)
                Tasks.RemoveAt(i);
        }

        // --------------------------------------------------------------------------------------------------------
    }

    // ############################################################################################################

    public delegate void TaskHandler(TimedTask taskInfo, object data);

    // ############################################################################################################
    // Helper Classes

    /// <summary>
    /// A task handler is called once the tick counter reaches the tick delay.
    /// Since the granular rate of ticks is usually at a fixed rate, such as every 1 ms, or 10 ms, etc., task delays
    /// are in "number of ticks". See SecondsToTicks() and MillisecondsToTicks() in SWTasks for some quick conversion
    /// methods.
    /// </summary>
    public class TimedTask
    {
        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Number of ticks (controller heart beats) before a task is run.
        /// A value of "0" calls a handler on next tick, and a value less than "0" specifies milliseconds
        /// instead of ticks (the value is converted into ticks).
        /// </summary>
        public double TickDelay
        {
            get { return _TickDelay; }
            set { _TickDelay = (value >= 0d) ? value : TimedTasks.MillisecondsToTicks(value); }
        }
        double _TickDelay = 0d;

        internal DateTime _LastTickTime; // (last time the tick occurred - used in case of lag)

        /// <summary>
        /// Returns the number of milliseconds since the last tick.
        /// </summary>
        public double TimeSinceLastTick { get { return (DateTime.Now - _LastTickTime).TotalMilliseconds; } }

        /// <summary>
        /// Returns the number of ticks that should have occurred since the last tick.
        /// </summary>
        public double TicksSinceLastTick { get { return TimedTasks.MillisecondsToTicks(TimeSinceLastTick); } }

        /// <summary>
        /// A task is executed when the number of timer 'ticks' equals the tick delay.
        /// This is always reset to zero before running the task.
        /// </summary>
        public double TickCounter = 0d; // (number of ticks elapsed; this is a double because lag throws off ticks by a fraction)

        /// <summary>
        /// The number of times to repeat a task. If this is -1, then the task repeats indefinitely.
        /// When a task is run, this value can be updated to force the task to repeat again if needed.
        /// </summary>
        public int RepeatCount = 0; // (number of times to repeat the task - if negative, the repetition is infinite)

        /// <summary>
        /// The call-back method to execute for the task.
        /// </summary>
        public TaskHandler Handler = null;

        /// <summary>
        /// Any user-defined data for the task.
        /// </summary>
        public object Data = null; // (user supplied data, passed onto the handler when called)

        /// <summary>
        /// Suspends the task indefinitely, while set to 'true'.
        /// </summary>
        public bool Paused = false; // (if true, the task will be skipped, and the tick counter will not update)

        /// <summary>
        /// A list of child tasks related to this task. These dependent tasks are removed when this task is removed; however, they run independently.
        /// </summary>
        public List<TimedTask> DependentTasks { get { return _DependentTasks ?? (_DependentTasks = new List<TimedTask>()); } }
        List<TimedTask> _DependentTasks;

        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Occurs when a task will no longer run and will be removed.
        /// </summary>
        public event Action<TimedTask> Finally;

        // --------------------------------------------------------------------------------------------------------

        public TimedTask(int delay, TaskHandler handler)
        {
            TickDelay = -delay;
            Handler = handler;
        }

        public TimedTask(int delay, TaskHandler handler, object data)
            : this(delay, handler)
        { Data = data; }

        public TimedTask(int delay, int repeatCount, TaskHandler handler, object data)
            : this(delay, handler, data)
        { RepeatCount = repeatCount; }

        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Executes the task (call-back method) immediately.
        /// If the tick counter is not reset,  then the task will run at the next scheduled tick count (which
        /// may be immediately after returning control back to the Silverlight message loop).
        /// </summary>
        /// <param name="resetTickCounter">To execute the task without interrupting the next scheduled run, or if the task should only run once, set this to 'false'.</param>
        public void Run(bool resetTickCounter)
        {
            if (Handler != null)
            {
                if (resetTickCounter)
                {
                    // ... caller has requested to reset and start over ...
                    TickCounter = 0d;
                    _LastTickTime = DateTime.Now;
                }
                else
                {
                    if (RepeatCount < 0 || RepeatCount > 0)
                    {
                        if (RepeatCount > 0) RepeatCount--;
                        else if (RepeatCount < -1) RepeatCount = -1;
                    }
                    else
                        Delete();
                }

                Handler(this, Data);
            }
        }
        public void Run() { Run(true); }

        // --------------------------------------------------------------------------------------------------------

        internal void _DoFinally() { if (Finally != null) Finally(this); }

        // --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Removes the task from the task queue.
        /// </summary>
        public void Delete() {
            TimedTasks.RemoveTask(this);

            if (_DependentTasks != null)
                foreach (var task in _DependentTasks)
                    task.Delete();
        }

        // --------------------------------------------------------------------------------------------------------
    }

    // ############################################################################################################
}
