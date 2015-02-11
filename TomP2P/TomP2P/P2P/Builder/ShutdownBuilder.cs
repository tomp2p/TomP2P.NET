using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Connection.Windows;
using TomP2P.Extensions.Workaround;

namespace TomP2P.P2P.Builder
{
    /// <summary>
    /// Sets the configuration options for the shutdown command. The shutdown does first a routing, searches
    /// for its close peers and then sends a quit message so that the other peers know that this peer is offline.
    /// </summary>
    public class ShutdownBuilder : DefaultConnectionConfiguration, ISignatureBuilder<ShutdownBuilder>
    {
        private static readonly Task TaskShutdown;

        private readonly Peer _peer;
        private KeyPair _keyPair;
        private RoutingConfiguration _routingConfiguration;
        private bool _forceRoutingOnlyToSelf = false;

        // static constructor
        static ShutdownBuilder()
        {
            var tcsShutdown = new TaskCompletionSource<object>();
            tcsShutdown.SetException(new TaskFailedException("Shutdown."));
            TaskShutdown = tcsShutdown.Task;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peer">The peer that runs the routing and quit messages.</param>
        public ShutdownBuilder(Peer peer)
        {
            _peer = peer;
        }

        /// <summary>
        /// Starts the shutdown.
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            if (_peer.IsShutdown)
            {
                return TaskShutdown;
            }
            SetIsForceUdp();
            if (_routingConfiguration == null)
            {
                _routingConfiguration = new RoutingConfiguration(8, 10, 2);
            }

            int conn = _routingConfiguration.Parallel;
            var taskCc = _peer.ConnectionBean.Reservation.CreateAsync(conn, 0);
            var tcsShutdownDone = new TaskCompletionSource<object>();
            Utils.Utils.AddReleaseListener(taskCc, tcsShutdownDone.Task);

            taskCc.ContinueWith(tcc =>
            {
                if (!tcc.IsFaulted)
                {
                    var cc = tcc.Result;
                    var routingBuilder = BootstrapBuilder.
                    // TODO implement
                    throw new NotImplementedException();
                }
                else
                {
                    if (tcc.Exception != null)
                    {
                        tcsShutdownDone.SetException(tcc.Exception);
                    }
                    else
                    {
                        tcsShutdownDone.SetException(new TaskFailedException("Task<ChannelCreator> failed."));
                    }
                }
            });

            return tcsShutdownDone.Task;
        }
    }
}
