using System.Threading.Tasks;

namespace TomP2P.Futures
{
    public class TaskWrapper<TTask> : BaseTaskImpl where TTask : Task
    {
        private TTask _wrappedTask;

        /// <summary>
        /// Wait for the task which will cause this task to complete if the wrapped
        /// task completes.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        public void WaitFor(TTask task)
        {
            // TODO self
            lock (Lock)
            {
                _wrappedTask = task;
            }
            task.ContinueWith(t =>
            {
                lock (Lock)
                {
                    if (!CompletedAndNotify())
                    {
                        return;
                    }
                    // TODO type and reason needed?
                }
                NotifyListeners();
            });
        }

        public TTask WrappedTask
        {
            get
            {
                lock (Lock)
                {
                    return _wrappedTask;
                }
            }
        }
    }
}
