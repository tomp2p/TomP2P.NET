using System.Collections.Generic;
using System.Threading.Tasks;

namespace TomP2P.Futures
{
    /// <summary>
    /// Equivalent for Java's FutureLateJoin.
    /// </summary>
    public class TaskLateJoin<TTask> : TaskCompletionSource<object> where TTask : Task
    {
        // K = FutureResponse = Task<Message>
        // FutureLateJoin = Task.WhenAll(FutureResponse[]) = Task.WhenAll(Task<Message>[])
        // = Task<Message[]>
        // --> TCS<Task<Message[]>>

        // base
        private readonly object _lock;
        private bool _completed = false;

        private readonly int _nrMaxTasks;
        private readonly int _minSuccess;

        private readonly IList<TTask> _tasksDone;
        private readonly IList<TTask> _tasksSubmitted;

        private TTask _lastSuccessTask;
        private int _successCount;

        public TaskLateJoin(int nrMaxTasks)
            : this(nrMaxTasks, nrMaxTasks)
        { }

        public TaskLateJoin(int nrMaxTasks, int minSuccess)
        {
            // base
            _lock = this;

            _nrMaxTasks = nrMaxTasks;
            _minSuccess = minSuccess;
            _tasksDone = new List<TTask>(nrMaxTasks);
            _tasksSubmitted = new List<TTask>(nrMaxTasks);
            // TODO self needed?
        }

        public bool Add(TTask task)
        {
            lock (_lock)
            {
                if (_completed)
                {
                    return false;
                }
                _tasksSubmitted.Add(task);
                task.ContinueWith(t =>
                {
                    bool done = false;
                    lock (_lock)
                    {
                        if (!_completed)
                        {
                            if (!task.IsFaulted)
                            {
                                _successCount++;
                                _lastSuccessTask = task;
                            }
                            _tasksDone.Add(task);
                            done = CheckDone();
                        }
                    }
                    if (done)
                    {
                        this.SetResult(null); // "notify listeners"
                    }
                });
                return true;
            }
        }

        private bool CheckDone()
        {
            if (_tasksDone.Count >= _nrMaxTasks || _successCount >= _minSuccess)
            {
                if (!Completed())
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        // base
        private bool Completed()
        {
            if (!_completed)
            {
                _completed = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the finished tasks.
        /// </summary>
        /// <returns></returns>
        public IList<TTask> TasksDone()
        {
            lock (_lock)
            {
                return _tasksDone;
            }
        }

        /// <summary>
        /// Returns the submitted tasks.
        /// </summary>
        /// <returns></returns>
        public IList<TTask> TasksSubmitted()
        {
            lock (_lock)
            {
                return _tasksSubmitted;
            }
        }

        /// <summary>
        /// Returns the last successful finished task.
        /// </summary>
        /// <returns></returns>
        public TTask LastSuccessTask()
        {
            lock (_lock)
            {
                return _lastSuccessTask;
            }
        }
    }
}
