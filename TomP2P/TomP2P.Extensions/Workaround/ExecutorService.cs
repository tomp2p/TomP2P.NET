using System.Threading;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// This class wraps several timer implementations of the .NET framework. These are grouped
    /// because there is no class with a full feature set that is similar to Java's ScheduledExecutorService,
    /// so instead a context-specific timer implementation can be used.
    /// </summary>
    public class ExecutorService
    {
        //public System.Threading.Timer ThreadingTimer { get; private set; }
        //public System.Timers.Timer TimersTimer { get; private set; }
        //public CancellationTokenSource CancellationTokenSource { get; private set; }

        /*public ExecutorService()
        {
            ThreadingTimer = new Timer();
            TimersTimer = new System.Timers.Timer();
        }*/

        public System.Threading.Timer ScheduleAtFixedRate(TimerCallback callback, object state, long dueTime, long period)
        {
            return new Timer(callback, state, dueTime, period);
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
