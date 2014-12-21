using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.P2P;

namespace TomP2P.Rpc
{
    /// <summary>
    /// The Ping message handler. Also used for NAT detection and other things.
    /// </summary>
    public class PingRpc
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static readonly int WaitTime = 10*1000;

        private readonly IList<IPeerReachable> _reachableListeners = new List<IPeerReachable>(1);
        private readonly IList<IPeerReceivedBroadcastPing> _receivedBroadcastPingListeners = new List<IPeerReceivedBroadcastPing>(1);

        // TODO needed?
        // used for testing and debugging 
        private readonly bool _enable;
        private readonly bool _wait;

        /// <summary>
        /// Creates a new handshake RPC with listeners.
        /// </summary>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        public PingRpc(PeerBean peerBean, ConnectionBean connectionBean)
            : this(peerBean, connectionBean, true, true, false)
        { }

        /// <summary>
        /// Constructor that is only called from this class or from test cases.
        /// </summary>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        /// <param name="enable">Used for test cases, set to true in production.</param>
        /// <param name="register">Used for test cases, set to true in production.</param>
        /// <param name="wait">Used for test cases, set to false in production.</param>
        private PingRpc(PeerBean peerBean, ConnectionBean connectionBean, bool enable, bool register, bool wait)
        {
            
        }
    }
}
