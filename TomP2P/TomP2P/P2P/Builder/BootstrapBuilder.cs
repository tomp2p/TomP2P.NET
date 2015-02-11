using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Connection.Windows;
using TomP2P.Peers;

namespace TomP2P.P2P.Builder
{
    /// <summary>
    /// Boostraps to a known peer. First, channels are reserved, then Discover(PeerAddress) is called to verify this Internet
    /// connection settings using the "peerAddress" argument . Then the routing is initiated to the peers specified in
    /// "bootstrapTo". Please be aware that in order to boostrap, you need to know the peer ID of all peers in the "bootstrapTo" collection. 
    /// Passing Number160.ZERO does *not* work.
    /// </summary>
    public class BootstrapBuilder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // TODO do these need to be of type "FutureWrapper"?
        private static readonly Task<IEnumerable<PeerAddress>> TaskBootstrapShutdown;
        private static readonly Task<IEnumerable<PeerAddress>> TaskBootstrapNoAddress;

        private readonly Peer _peer;
        private IEnumerable<PeerAddress> _bootstrapTo;
        private PeerAddress _peerAddress;
        private IPEndPoint _inetAddress;
        private int _portUdp = Ports.DefaultPort;
        private int _portTcp = Ports.DefaultPort;
        private RoutingConfiguration _routingConfiguration;
        private bool _forceRoutingOnlyToSelf = false;
        private bool _broadcast = false;

        // static constructor
        static BootstrapBuilder()
        {
            var tcsBootstrapShutdown = new TaskCompletionSource<IEnumerable<PeerAddress>>();
            tcsBootstrapShutdown.SetException(new TaskFailedException("Peer is shutting down."));
            TaskBootstrapShutdown = tcsBootstrapShutdown.Task;

            var tcsBootstrapNoAddress = new TaskCompletionSource<IEnumerable<PeerAddress>>();
            tcsBootstrapNoAddress.SetException(new TaskFailedException("No addresses to bootstrap to have been provided. Or maybe, the provided address has peer ID set to zero."));
            TaskBootstrapNoAddress = tcsBootstrapNoAddress.Task;
        }


    }
}
