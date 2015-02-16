using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Extensions;
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
        /// <summary>
        /// The current key pair to sign the message. If null, no signature is applied.
        /// </summary>
        public KeyPair KeyPair { get; private set; }
        public RoutingConfiguration RoutingConfiguration { get; private set; }
        public bool IsForceRoutingOnlyToSelf { get; private set; }

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
        public Task StartAsync()
        {
            if (_peer.IsShutdown)
            {
                return TaskShutdown;
            }
            SetIsForceUdp();
            if (RoutingConfiguration == null)
            {
                RoutingConfiguration = new RoutingConfiguration(8, 10, 2);
            }

            int conn = RoutingConfiguration.Parallel;
            var taskCc = _peer.ConnectionBean.Reservation.CreateAsync(conn, 0);
            var tcsShutdownDone = new TaskCompletionSource<object>();
            Utils.Utils.AddReleaseListener(taskCc, tcsShutdownDone.Task);

            taskCc.ContinueWith(tcc =>
            {
                if (!tcc.IsFaulted)
                {
                    var cc = tcc.Result;
                    var routingBuilder = BootstrapBuilder.CreateBuilder(RoutingConfiguration, IsForceRoutingOnlyToSelf);
                    routingBuilder.LocationKey = _peer.PeerId;
                    
                    var tcsRouting = _peer.DistributedRouting.Quit(routingBuilder, cc);
                    tcsRouting.Task.ContinueWith(taskRouting =>
                    {
                        if (!taskRouting.IsFaulted)
                        {
                            tcsShutdownDone.SetResult(null); // complete
                        }
                        else
                        {
                            tcsShutdownDone.SetException(taskRouting.TryGetException());
                        }
                    });
                }
                else
                {
                    tcsShutdownDone.SetException(tcc.TryGetException());
                }
            });

            return tcsShutdownDone.Task;
        }

        /// <summary>
        /// Gets whether the message should be signed.
        /// </summary>
        public bool IsSign
        {
            get { return KeyPair != null; }
        }

        public ShutdownBuilder SetSign(bool signMessage)
        {
            if (signMessage)
            {
                SetSign();
            }
            else
            {
                KeyPair = null;
            }
            return this;
        }

        public ShutdownBuilder SetSign()
        {
            KeyPair = _peer.PeerBean.KeyPair;
            return this;
        }

        public ShutdownBuilder SetKeyPair(KeyPair keyPair)
        {
            KeyPair = keyPair;
            return this;
        }

        public ShutdownBuilder SetRoutingConfiguration(RoutingConfiguration routingConfiguration)
        {
            RoutingConfiguration = routingConfiguration;
            return this;
        }

        public ShutdownBuilder SetIsForceRoutingOnlyToSelf()
        {
            return SetIsForceRoutingOnlyToSelf(true);
        }

        public ShutdownBuilder SetIsForceRoutingOnlyToSelf(bool isForceRoutingOnlyToSelf)
        {
            IsForceRoutingOnlyToSelf = isForceRoutingOnlyToSelf;
            return this;
        }
    }
}
