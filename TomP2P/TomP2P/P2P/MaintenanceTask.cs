using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions.Workaround;
using TomP2P.Peers;

namespace TomP2P.P2P
{
    public class MaintenanceTask
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const int MaxPing = 5;
        private static readonly VolatileInteger Counter = new VolatileInteger(0);

        private Peer _peer;
        public int IntervalMillis { get; private set; }
        private readonly IList<IMaintainable> _maintainables = new List<IMaintainable>();
        private readonly IDictionary<Task, PeerAddress> _runningTasks = new Dictionary<Task, PeerAddress>();
        private bool _shutdown = false;
        private readonly object _lock = new object();

        private Timer _timer;

        public MaintenanceTask()
        {
            IntervalMillis = 1000;
        }

        public void Init(Peer peer, ExecutorService timer)
        {
            _peer = peer;
            _timer = timer.ScheduleAtFixedRate(Run, null, IntervalMillis, IntervalMillis);

            // MSDN: The method can be executed simultaneously on two thread pool threads 
            // if the timer interval is less than the time required to execute the method, or
            // if all thread pool threads are in use and the method is queued multiple times.
        }

        private void Run(object state)
        {
            Logger.Debug("Maintenance Thread {0}: Maintenance triggered...", Thread.CurrentThread.ManagedThreadId);
            lock (_lock)
            {
                // make sure we only have 5 pings in parallel
                if (_shutdown || Counter.Get() > MaxPing)
                {
                    return;
                }
                foreach (var maintainable in _maintainables)
                {
                    var peerStatistic = maintainable.NextForMaintenance(_runningTasks.Values);
                    if (peerStatistic == null)
                    {
                        return;
                    }
                    var task = _peer.Ping().SetPeerAddress(peerStatistic.PeerAddress).Start();
                    Logger.Debug("Maintenance ping from {0} to {1}.", _peer.PeerAddress, peerStatistic.PeerAddress);

                    _peer.NotifyAutomaticFutures(task);
                    _runningTasks.Add(task, peerStatistic.PeerAddress);
                    Counter.IncrementAndGet();
                    task.ContinueWith(t =>
                    {
                        lock (_lock)
                        {
                            _runningTasks.Remove(task);
                            Counter.Decrement();
                        }
                    });
                }
            }
        }

        public Task ShutdownAsync()
        {
            if (_timer != null)
            {
                // .NET-specific
                ExecutorService.Cancel(_timer);
            }
            var tcsShutdown = new TaskCompletionSource<object>();
            lock (_lock)
            {
                _shutdown = true;
                int max = _runningTasks.Count;
                if (max == 0)
                {
                    tcsShutdown.SetResult(null);
                    return tcsShutdown.Task;
                }
                var counter = new VolatileInteger(0);
                foreach (var task in _runningTasks.Keys)
                {
                    task.ContinueWith(t =>
                    {
                        if (counter.IncrementAndGet() == max)
                        {
                            tcsShutdown.SetResult(null); // complete
                        }
                    });
                }
            }

            return tcsShutdown.Task;
        }

        public void AddMaintainable(IMaintainable maintainable)
        {
            _maintainables.Add(maintainable);
        }

        public MaintenanceTask SetIntervalMillis(int intervalMillis)
        {
            IntervalMillis = intervalMillis;
            return this;
        }
    }
}
