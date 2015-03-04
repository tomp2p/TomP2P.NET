using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseServer : BaseChannel, IServerChannel
    {
        private Task[] _tasks;

        protected BaseServer(IPEndPoint localEndPoint, Pipeline pipeline)
            : base (localEndPoint, pipeline)
        { }

        public void Start()
        {
            DoStart();

            // accept MaxNrOfClients simultaneous connections
            var maxNrOfClients = 1; // TODO Utils.Utils.GetMaxNrOfClients();
            // TODO find better way of initiating service loops (thread pool)
            _tasks = new Task[maxNrOfClients];
            for (int i = 0; i < maxNrOfClients; i++)
            {
                _tasks[i] = ServiceLoopAsync(CloseToken);
            }
        }

        public async Task StopAsync()
        {
            Close();

            // TODO await closing of all service-loops
            /*if (_tasks != null)
            {
                await Task.WhenAll(_tasks);
            }*/
        }

        public abstract void DoStart();

        public abstract Task ServiceLoopAsync(CancellationToken ct);
    }
}
