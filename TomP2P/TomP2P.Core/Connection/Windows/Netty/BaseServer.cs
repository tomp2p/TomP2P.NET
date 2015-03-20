using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Core.Connection.Windows.Netty
{
    public abstract class BaseServer : BaseChannel, IServerChannel
    {
        private Task[] _tasks;

        protected BaseServer(IPEndPoint localEndPoint, Pipeline pipeline)
            : base (localEndPoint, pipeline)
        { }

        public void Start()
        {
            // accept MaxNrOfClients simultaneous connections
            var maxNrOfClients = Utils.Utils.GetMaxNrOfClients();
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

        protected override void DoClose()
        {
            // nothing to do
        }

        public abstract Task ServiceLoopAsync(CancellationToken ct);
    }
}
