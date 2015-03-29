using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace TomP2P.Core.Connection.Windows.Netty
{
    public abstract class BaseServer : BaseChannel, IServerChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected BaseServer(IPEndPoint localEndPoint, Pipeline pipeline)
            : base (localEndPoint, pipeline)
        { }

        public void Start()
        {
            ThreadPool.QueueUserWorkItem(async delegate
            {
                try
                {
                    await ServiceLoopAsync(CloseToken);
                }
                catch (Exception ex)
                {
                    Logger.Error("An exception occurred in the server's service loop.", ex);
                    throw;
                }
            }, CloseToken);
        }

        public void Stop()
        {
            Close();
        }

        protected override void DoClose()
        {
            // nothing to do
        }

        protected abstract Task ServiceLoopAsync(CancellationToken ct);

        protected abstract Task ProcessRequestAsync(object state);
    }
}
