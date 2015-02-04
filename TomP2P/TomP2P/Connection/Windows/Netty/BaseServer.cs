using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseServer : BaseChannel
    {
        private CancellationTokenSource _cts;
        private Task[] _tasks;

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
            if (_tasks != null)
            {
                _cts.Cancel();
                await Task.WhenAll(_tasks);
            }
        }

        protected abstract Task ServiceLoopAsync(CancellationToken ct);
    }
}
