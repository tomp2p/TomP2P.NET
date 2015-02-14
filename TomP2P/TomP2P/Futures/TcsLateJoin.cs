using System.Collections.Generic;
using System.Threading.Tasks;

namespace TomP2P.Futures
{
    /// <summary>
    /// Equivalent for Java's FutureLateJoin.
    /// </summary>
    public class TcsLateJoin<TTask> : BaseTcsImpl where TTask : Task
    {
        // K = FutureResponse = Task<Message>
        // FutureLateJoin = Task.WhenAll(FutureResponse[]) = Task.WhenAll(Task<Message>[])
        // = Task<Message[]>
        // --> TCS<Task<Message[]>>

        private readonly int _nrMaxTasks;
        private readonly int _minSuccess;

        private readonly IList<TTask> _tasksDone;
        private readonly IList<TTask> _tasksSubmitted;

        private TTask _lastSuccessTask;
        private int _successCount;

        public TcsLateJoin(int nrMaxTasks)
            : this(nrMaxTasks, nrMaxTasks)
        { }

        public TcsLateJoin(int nrMaxTasks, int minSuccess)
        {
            _nrMaxTasks = nrMaxTasks;
            _minSuccess = minSuccess;
            _tasksDone = new List<TTask>(nrMaxTasks);
            _tasksSubmitted = new List<TTask>(nrMaxTasks);
            // TODO self needed?
        }

        public bool Add(TTask task)
        {
            lock (Lock)
            {
                if (Completed)
                {
                    return false;
                }
                _tasksSubmitted.Add(task);
                task.ContinueWith(t =>
                {
                    bool done = false;
                    lock (Lock)
                    {
                        if (!Completed)
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
                        NotifyListeners();
                    }
                });
                return true;
            }
        }

        private bool CheckDone()
        {
            if (_tasksDone.Count >= _nrMaxTasks || _successCount >= _minSuccess)
            {
                if (!CompletedAndNotify())
                {
                    return false;
                }
                return true;
            }
            return false;
        }



        /// <summary>
        /// Returns the finished tasks.
        /// </summary>
        /// <returns></returns>
        public IList<TTask> TasksDone()
        {
            lock (Lock)
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
            lock (Lock)
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
            lock (Lock)
            {
                return _lastSuccessTask;
            }
        }
    }
}
