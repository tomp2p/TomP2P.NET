using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseServer : BaseChannel, IServerChannel
    {
        private CancellationTokenSource _cts;
        private Task[] _tasks;

        protected BaseServer(IPEndPoint localEndPoint)
            : base (localEndPoint)
        { }

        public void Start()
        {
            DoStart();

            // accept MaxNrOfClients simultaneous connections
            var maxNrOfClients = Utils.Utils.GetMaxNrOfClients();
            _tasks = new Task[maxNrOfClients];
            _cts = new CancellationTokenSource();
            for (int i = 0; i < maxNrOfClients; i++)
            {
                _tasks[i] = ServiceLoopAsync(_cts.Token);
            }
        }

        public abstract void DoStart();

        public async Task StopAsync()
        {
            Close();
            if (_cts != null)
            {
                _cts.Cancel();
            }
            // TODO await closing of all service-loops
            /*if (_tasks != null)
            {
                await Task.WhenAll(_tasks);
            }*/
        }

        public abstract Task ServiceLoopAsync(CancellationToken ct);
    }
}
