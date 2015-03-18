using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    /// <summary>
    /// Custom task scheduler that limites the number of threads used by the application.
    /// Inspired by https://msdn.microsoft.com/en-us/library/ee789351%28v=vs.110%29.aspx
    /// </summary>
    public sealed class LimitedConcurrenctyTaskScheduler : TaskScheduler
    {
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;

        private readonly LinkedList<Task> _tasks = new LinkedList<Task>();
        private readonly int _concurrency;

        private int _delegatesQueuedOrRunning = 0;

        public LimitedConcurrenctyTaskScheduler(int concurrency)
        {
            if (concurrency < 1)
            {
                throw new ArgumentOutOfRangeException("concurrency");
            }
            _concurrency = concurrency;
        }

        protected override void QueueTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.AddLast(task);
                if (_delegatesQueuedOrRunning < _concurrency)
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                _currentThreadIsProcessingItems = true;
                try
                {
                    // process all available tasks in the queue
                    while (true)
                    {
                        Task task;
                        lock (_tasks)
                        {
                            if (_tasks.Count == 0)
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            task = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // execute task
                        TryExecuteTask(task);
                    }
                }
                finally
                {
                    _currentThreadIsProcessingItems = false;
                }
            }, null);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // if this thread is not already processing a task, inlining is supported
            if (!_currentThreadIsProcessingItems)
            {
                return false;
            }

            // if task was previously queued, remove it from queue
            if (taskWasPreviouslyQueued)
            {
                // try to run the task
                if (TryDequeue(task))
                {
                    return TryExecuteTask(task);
                }
                return false;
            }
            return TryExecuteTask(task);
        }

        protected override bool TryDequeue(Task task)
        {
            lock (_tasks)
            {
                return _tasks.Remove(task);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken)
                {
                    return _tasks;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_tasks);
                }
            }
        }
    }
}
