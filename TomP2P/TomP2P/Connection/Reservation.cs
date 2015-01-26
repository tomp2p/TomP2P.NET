using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TomP2P.Connection.NET_Helper;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Connection
{
    /// <summary>
    /// Reserves a block of connections.
    /// </summary>
    public class Reservation
    {
        private readonly int _maxPermitsUdp;
        private readonly int _maxPermitsTcp;
        private readonly int _maxPermitsPermanentTcp;

        private readonly Semaphore _semaphoreUdp;
        private readonly Semaphore _semaphoreTcp;
        private readonly Semaphore _semaphorePermanentTcp;

        private readonly ChannelClientConfiguration _channelClientConfiguration;

        // TODO check if best equivalent
        private readonly ConcurrentQueue<Task> _queue = new ConcurrentQueue<Task>();

        // single thread
        // TODO this class uses a threadpool that is not limited to 1 single thread!
        // TODO find ExecutorService equivalent

        // we should be fair, otherwise, we see connection timeouts
        // due to unfairness if busy
        private readonly ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private bool _shutdown = false;

        private readonly SynchronizedCollection<ChannelCreator> _channelCreators = new SynchronizedCollection<ChannelCreator>();

        private readonly TaskCompletionSource<object> _tcsReservationDone = new TaskCompletionSource<object>();

        /// <summary>
        /// Creates a new reservation class with the 3 permits contained in the provided configuration.
        /// </summary>
        /// <param name="channelClientConfiguration">Contains the 3 permits:
        /// - MaxPermitsUdp: The number of maximum short-lived UDP connections.
        /// - MaxPermitsTcp: The number of maximum short-lived TCP connections.
        /// - MaxPermitsPermanentTcp: The number of maximum permanent TCP connections.</param>
        public Reservation(ChannelClientConfiguration channelClientConfiguration)
        {
            _maxPermitsUdp = channelClientConfiguration.MaxPermitsUdp;
            _maxPermitsTcp = channelClientConfiguration.MaxPermitsTcp;
            _maxPermitsPermanentTcp = channelClientConfiguration.MaxPermitsPermanentTcp;
            _semaphoreUdp = new Semaphore(_maxPermitsUdp, _maxPermitsUdp);
            _semaphoreTcp = new Semaphore(_maxPermitsTcp, _maxPermitsTcp);
            _semaphorePermanentTcp = new Semaphore(0, _maxPermitsPermanentTcp);
            _channelClientConfiguration = channelClientConfiguration;
        }

        // TODO implement the second Create() method

        /// <summary>
        /// Creates a channel creator for short-lived connections.
        /// Always call <see cref="ChannelCreator.ShutdownAsync"/> to release all resources.
        /// (This needs to be done in any case, whether it succeeds or fails.)
        /// </summary>
        /// <param name="permitsUdp">The number of short-lived UDP connections.</param>
        /// <param name="permitsTcp">The number of short-lived TCP connections.</param>
        /// <returns>The future channel creator.</returns>
        public Task<ChannelCreator> CreateAsync(int permitsUdp, int permitsTcp)
        {
            if (permitsUdp > _maxPermitsUdp)
            {
                throw new ArgumentException(String.Format("Cannot acquire more UDP connections ({0}) than maximally allowed ({1}).", permitsUdp, _maxPermitsUdp));
            }
            if (permitsTcp > _maxPermitsTcp)
            {
                throw new ArgumentException(String.Format("Cannot acquire more TCP connections ({0}) than maximally allowed ({1}).", permitsTcp, _maxPermitsTcp));
            }
            var tcsChannelCreator = new TaskCompletionSource<ChannelCreator>();
            _readWriteLock.EnterReadLock();
            try
            {
                if (_shutdown)
                {
                    tcsChannelCreator.SetException(new TaskFailedException("Shutting down."));
                    return tcsChannelCreator.Task;
                }

                var tcsChannelCreationDone = new TaskCompletionSource<object>();
                tcsChannelCreationDone.Task.ContinueWith(delegate
                {
                    // release the permits in all cases
                    // otherwise, we may see inconsistencies
                    _semaphoreUdp.Release2(permitsUdp);
                    _semaphoreTcp.Release2(permitsTcp);
                });

                // instead of Executor.execute(new WaitReservation())
                ThreadPool.QueueUserWorkItem(delegate
                {
                    // Creates a reservation that returns a channel creator in a
                    // task, once we have the semaphore.
                    // Tries to reserve a channel creator. If too many channels are already
                    // created, wait until channels are closed.

                    ChannelCreator channelCreator;
                    _readWriteLock.EnterReadLock();
                    try
                    {
                        if (_shutdown)
                        {
                            tcsChannelCreator.SetException(new TaskFailedException("Shutting down."));
                            return;
                        }
                        try
                        {
                            _semaphoreUdp.Acquire(permitsUdp);
                        }
                        catch (Exception ex)
                        {
                            tcsChannelCreator.SetException(ex);
                            return;
                        }
                        try
                        {
                            _semaphoreTcp.Acquire(permitsTcp);
                        }
                        catch (Exception ex)
                        {
                            _semaphoreUdp.Release(permitsUdp);
                            tcsChannelCreator.SetException(ex);
                            return;
                        }

                        channelCreator = new ChannelCreator(tcsChannelCreationDone, permitsUdp, permitsTcp,
                            _channelClientConfiguration);
                        AddToSet(channelCreator);
                    }
                    finally
                    {
                        _readWriteLock.ExitReadLock();
                    }
                    tcsChannelCreator.SetResult(channelCreator);
                });

                return tcsChannelCreator.Task;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Creates a channel creator for permanent TCP connections.
        /// </summary>
        /// <param name="permitsPermanentTcp">The number of long-lived TCP connections.</param>
        /// <returns>The future channel creator.</returns>
        public Task<ChannelCreator> CreatePermanentAsync(int permitsPermanentTcp)
        {
            if (permitsPermanentTcp > _maxPermitsPermanentTcp)
            {
                throw new ArgumentException(String.Format("Cannot acquire more permantent TCP connections ({0}) than maximally allowed ({1}).", permitsPermanentTcp, _maxPermitsPermanentTcp));
            }
            var tcsChannelCreator = new TaskCompletionSource<ChannelCreator>();
            _readWriteLock.EnterReadLock();
            try
            {
                if (_shutdown)
                {
                    tcsChannelCreator.SetException(new TaskFailedException("Shutting down."));
                    return tcsChannelCreator.Task;
                }

                var tcsChannelCreationDone = new TaskCompletionSource<object>();
                tcsChannelCreationDone.Task.ContinueWith(delegate
                {
                    // release the permits in all cases
                    // otherwise, we may see inconsistencies
                    _semaphorePermanentTcp.Release(permitsPermanentTcp);
                });

                // instead of Executor.execute(new WaitReservationPermanent())
                ThreadPool.QueueUserWorkItem(delegate
                {
                    // Creates a reservation that returns a channel creator in a
                    // task, once we have the semaphore.
                    // Tries to reserve a channel creator. If too many channels are already
                    // created, wait until channels are closed.
                    ChannelCreator channelCreator;
                    _readWriteLock.EnterReadLock();
                    try
                    {
                        if (_shutdown)
                        {
                            tcsChannelCreator.SetException(new TaskFailedException("Shutting down."));
                        }

                        try
                        {
                            _semaphorePermanentTcp.Acquire(permitsPermanentTcp);
                        }
                        catch (Exception ex)
                        {
                            tcsChannelCreator.SetException(ex);
                            return;
                        }

                        channelCreator = new ChannelCreator(tcsChannelCreationDone, 0, permitsPermanentTcp, _channelClientConfiguration);
                        AddToSet(channelCreator);
                    }
                    finally
                    {
                        _readWriteLock.ExitReadLock();
                    }
                    tcsChannelCreator.SetResult(channelCreator);
                });

                return tcsChannelCreator.Task;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Shuts down all the channel creators.
        /// </summary>
        /// <returns></returns>
        public Task ShutdownAsync()
        {
            _readWriteLock.EnterWriteLock();
            try
            {
                if (_shutdown)
                {
                    _tcsReservationDone.SetException(new TaskFailedException("Already shutting down."));
                    return _tcsReservationDone.Task;
                }
                _shutdown = true;
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }

            // Fast shutdown for those that are in the queue is not required.
            // Let the executor finish since the shutdown-flag is set and the
            // future will be set as well to "shutting down".
            
            // TODO find a way to abort the un-started tasks in the thread pool/executor

            // the channel creator doesn't change anymore from here on // TODO correct?
            int size = _channelCreators.Count;
            if (size == 0)
            {
                _tcsReservationDone.SetResult(null);
            }
            else
            {
                var completeCounter = new AtomicInteger(0);
                foreach (var channelCreator in _channelCreators)
                {
                    // It's important to set the listener before calling shutdown.
                    channelCreator.ShutdownTask.ContinueWith(delegate
                    {
                        if (completeCounter.IncrementAndGet() == size)
                        {
                            // we can block here
                            _semaphoreUdp.Acquire(_maxPermitsUdp);
                            _semaphoreTcp.Acquire(_maxPermitsTcp);
                            _semaphorePermanentTcp.Acquire(_maxPermitsPermanentTcp);
                            _tcsReservationDone.SetResult(null);
                        }
                    });
                }
            }
            return _tcsReservationDone.Task;
        }

        /// <summary>
        /// Adds a channel creator to the set and also adds it to the shutdown listener.
        /// </summary>
        /// <param name="channelCreator"></param>
        private void AddToSet(ChannelCreator channelCreator)
        {
            channelCreator.ShutdownTask.ContinueWith(delegate
            {
                _readWriteLock.EnterReadLock();
                try
                {
                    if (_shutdown)
                    {
                        return;
                    }
                    _channelCreators.Remove(channelCreator);
                }
                finally
                {
                    _readWriteLock.ExitReadLock();
                }
            });
            _channelCreators.Add(channelCreator);
        }

        /// <summary>
        /// Gets the pending number of requests that are scheduled but not executed yet.
        /// </summary>
        public int PendingRequests
        {
            get { return _queue.Count; }
        }
    }
}
