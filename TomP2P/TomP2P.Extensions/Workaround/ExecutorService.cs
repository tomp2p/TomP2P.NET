using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// This class wraps several timer implementations of the .NET framework. These are grouped
    /// because there is no class with a full feature set that is similar to Java's ScheduledExecutorService,
    /// so instead a context-specific timer implementation can be used.
    /// </summary>
    public class ExecutorService
    {
        private readonly IList<Timer> _scheduledTasks = new List<Timer>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Creates and executes a periodic action that becomes enabled first after the 
        /// given initial delay, and subsequently with the given period.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="dueTime"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public Timer ScheduleAtFixedRate(TimerCallback callback, object state, long dueTime, long period)
        {
            var timer = new Timer(callback, state, dueTime, period);
            _scheduledTasks.Add(timer);
            return timer;
        }

        /// <summary>
        /// Creates and executes a one-shot action that becomes enabled after the given 
        /// delay.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="delayMs">The delay in milliseconds.</param>
        /// <returns></returns>
        public CancellationTokenSource Schedule(TimerCallback callback, object state, double delayMs)
        {
            if (_cts.IsCancellationRequested)
            {
                return null;
            }

            // cancel taskCts, if _cts is cancelled
            var taskCts = new CancellationTokenSource();
            _cts.Token.Register(taskCts.Cancel);

            var delay = Task.Delay(TimeSpan.FromMilliseconds(delayMs), taskCts.Token);
            delay.ContinueWith(taskDelay =>
            {
                if (!taskCts.IsCancellationRequested)
                {
                    // invoke callback
                    callback(state);
                }
            }, taskCts.Token);
            return taskCts;
        }

        /// <summary>
        /// Initiates an orderly shutdown in which previously submitted tasks are executed, but no
        /// new tasks will be accepted. Invocation has no additional effect if already shut down.
        /// This method does not wait for previously submitted tasks to complete execution.
        /// </summary>
        public void Shutdown()
        {
            _cts.Cancel();
            
            foreach (var scheduledTask in _scheduledTasks)
            {
                // non-blocking disposure
                scheduledTask.Dispose();
            }
        }

        /// <summary>
        /// Stops the provided timer and blocks until all callbacks have finished.
        /// </summary>
        /// <param name="timer">The timer to stop.</param>
        public static void Cancel(Timer timer)
        {
            // MSDN: Use this overload of the Dispose method if you want to be able to 
            // block until you are certain that the timer has been disposed. The timer
            // is not disposed until all currently queued callbacks have completed.
            var waitHandle = new AutoResetEvent(false);
            timer.Dispose(waitHandle);
            waitHandle.WaitOne();
        }
    }
}
