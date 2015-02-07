using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection.Windows;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Futures
{
    public class TaskForkJoin<TTask> : BaseTaskImpl where TTask : Task
    {
        private readonly VolatileReferenceArray<TTask> _forks;

        private readonly int _nrTasks;

        private readonly int _nrFinishTaskSuccess;

        private readonly bool _cancelTasksOnFinish;

        private readonly IList<TTask> _forksCopy = new List<TTask>();

        // all these values are accessed within synchronized blocks
        private int _counter = 0;
        private int _successCounter = 0;

        /// <summary>
        /// Facade if we expect everythin to return successfully.
        /// </summary>
        /// <param name="forks">The tasks that can also be modified outside this class.
        /// If a task is finished, the the task in that array will be set to null.
        /// A task may be initially null, which is considered a failure.</param>
        public TaskForkJoin(VolatileReferenceArray<TTask> forks)
            : this(forks.Length, false, forks)
        { }

        /// <summary>
        /// Creates a TaskForkJoin object.
        /// </summary>
        /// <param name="nrFinishFuturesSuccess">The number of tasks expected to succeed.</param>
        /// <param name="cancelTasksOnFinish">Whether the remaining tasks should be cancelled.
        /// For Get() it makes sense, for Store() it dose not.</param>
        /// <param name="forks">The tasks that can also be modified outside this class.
        /// If a task is finished, the the task in that array will be set to null.
        /// A task may be initially null, which is considered a failure.</param>
        public TaskForkJoin(int nrFinishFuturesSuccess, bool cancelTasksOnFinish, VolatileReferenceArray<TTask> forks)
        {
            _nrFinishTaskSuccess = nrFinishFuturesSuccess;
            _cancelTasksOnFinish = cancelTasksOnFinish;
            _forks = forks;

            // the task array may have null entries, so count first
            _nrTasks = forks.Length;
            if (_nrTasks <= 0)
            {
                // "failed"
                this.SetException(new TaskFailedException("We have no tasks: " + _nrTasks));
            }
            else
            {
                Join();
            }
            // TODO self needed?
        }

        /// <summary>
        /// Adds listeners and evaluates the result and when to notify the listeners.
        /// </summary>
        private void Join()
        {
            for (int i = 0; i < _nrTasks; i++)
            {
                lock (Lock)
                {
                    if (Completed)
                    {
                        return;
                    }
                }
                int index = i;
                if (_forks.Get(index) != null)
                {
                    var task = _forks.Get(index);
                    task.ContinueWith(t =>
                    {
                        Evaluate(task, index);
                    });
                }
                else
                {
                    bool notifyNow = false;
                    lock (Lock)
                    {
                        // if the counter reaches _nrTasks, that means we are finished
                        // and in this case, we failed otherwise, in evaluate,
                        // _successCounter would finish first
                        if (++_counter >= _nrTasks)
                        {
                            notifyNow = Finish();
                        }
                    }
                    if (notifyNow)
                    {
                        NotifyListeners();
                        CancelAll();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Evaluates one task and determines of this task is finished.
        /// </summary>
        /// <param name="finishedTask"></param>
        /// <param name="index"></param>
        private void Evaluate(TTask finishedTask, int index)
        {
            bool notifyNow = false;
            lock (Lock)
            {
                // this if-statement is very important
                // If the task is finished, then any subsequent evaluation, which
                // will happen as we add the listener in the join, must not set the task to null!
                if (Completed)
                {
                    return;
                }
                // add the task that we have evaluated
                _forksCopy.Add(finishedTask);
                _forks.Set(index, null);
                if (!finishedTask.IsFaulted && ++_successCounter >= _nrFinishTaskSuccess)
                {
                    notifyNow = Finish();
                }
                else if (++_counter >= _nrTasks)
                {
                    notifyNow = Finish();
                }
            }
            if (notifyNow)
            {
                NotifyListeners();
                CancelAll();
            }
        }

        private bool Finish() // TODO FutureType needed?
        {
            if (!CompletedAndNotify())
            {
                return false;
            }
            // TODO type, reason needed?
            return true;
        }

        /// <summary>
        /// Cancels all remaining tasks if requested by the user.
        /// </summary>
        private void CancelAll()
        {
            if (_cancelTasksOnFinish)
            {
                for (int i = 0; i < _nrTasks; i++)
                {
                    var task = _forks.Get(i);
                    if (task != null)
                    {
                        // TODO implement
                        throw new NotImplementedException();
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of evaluated tasks.
        /// </summary>
        public IList<TTask> CompletedTasks
        {
            get
            {
                lock (Lock)
                {
                    return _forksCopy;
                }
            }
        }

        public TTask First
        {
            get
            {
                lock (Lock)
                {
                    if (_forksCopy.Count != 0)
                    {
                        return _forksCopy[0];
                    }
                }
                return null;
            }
        }
    }
}
