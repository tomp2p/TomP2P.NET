using System.Collections.Generic;

namespace TomP2P.Peers
{
    /// <summary>
    /// The class that holds configuration settings for the <see cref="PeerMap"/>.
    /// </summary>
    public class PeerMapConfiguration
    {
        /// <summary>
        /// The peer ID of this peer.
        /// </summary>
        public Number160 Self { get; private set; }
        /// <summary>
        /// Each distance bit has its own bag.
        /// This is the size of the verified peers that are known to be online.
        /// </summary>
        public int BagSizeVerified { get; private set; }
        /// <summary>
        /// Each distance bit has its own bag.
        /// This is the size of the non-verified peers that may have been reported by other peers.
        /// </summary>
        public int BagSizeOverflow { get; private set; }
        /// <summary>
        /// The time, in seconds, a peer is considered offline. This is important, since we see that a peer is offline
        /// and another peer reports this peer, we don't want to add it to our map. Thus, there is a map that keeps 
        /// track of such peers. This also means that a fast reconnect is not possible and a peer has to wait until the 
        /// timeout to rejoin.
        /// </summary>
        public int OfflineTimeout { get; private set; }
        /// <summary>
        /// The time, in seconds, a peer is considered offline (shut down). This is important, since we see that a peer is offline
        /// and another peer reports this peer, we don't want to add it to our map. Thus, there is a map that keeps 
        /// track of such peers. This also means that a fast reconnect is not possible and a peer has to wait until the 
        /// timeout to rejoin.
        /// </summary>
        public int ShutdownTimeout { get; private set; }
        /// <summary>
        /// The time, in seconds, a peer is considered offline (exception). This is important, since we see that a peer is offline
        /// and another peer reports this peer, we don't want to add it to our map. Thus, there is a map that keeps 
        /// track of such peers. This also means that a fast reconnect is not possible and a peer has to wait until the 
        /// timeout to rejoin.
        /// </summary>
        public int ExceptionTimeout { get; private set; }
        /// <summary>
        /// The number of times that the peer is not reachable.
        /// Afther that, the peer is considered offline.
        /// </summary>
        public int OfflineCount { get; private set; }
        /// <summary>
        /// These filters can be set to not accept certain peers.
        /// </summary>
        public ICollection<IPeerFilter> PeerFilters { get; private set; }
        /// <summary>
        /// The instance that is responsible for maintenance.
        /// </summary>
        public IMaintenance Maintenance { get; private set; }
        public bool IsPeerVerification { get; private set; }

        /// <summary>
        /// Constructor with reasonable defaults.
        /// </summary>
        /// <param name="self">The peer ID of this peer.</param>
        public PeerMapConfiguration(Number160 self)
        {
            Self = self;
            BagSizeVerified = 10;
            BagSizeOverflow = 10;
            OfflineTimeout = 60;
            ShutdownTimeout = 20;
            ExceptionTimeout = 120;
            OfflineCount = 3;
            Maintenance = new DefaultMaintenance(4, new[] { 2, 4, 8, 16, 32, 64 });
            IsPeerVerification = true;

            PeerFilters = new List<IPeerFilter>(2);
        }

        public PeerMapConfiguration SetBagSizeVerified(int bagSizeVerified)
        {
            BagSizeVerified = bagSizeVerified;
            return this;
        }

        public PeerMapConfiguration SetBagSizeOverflow(int bagSizeOverflow)
        {
            BagSizeOverflow = bagSizeOverflow;
            return this;
        }

        public PeerMapConfiguration SetOfflineTimeout(int offlineTimeout)
        {
            OfflineTimeout = offlineTimeout;
            return this;
        }

        public PeerMapConfiguration SetOfflineCount(int offlineCount)
        {
            OfflineCount = offlineCount;
            return this;
        }

        public PeerMapConfiguration AddPeerFilter(IPeerFilter peerFilter)
        {
            PeerFilters.Add(peerFilter);
            return this;
        }

        public PeerMapConfiguration SetMaintenance(IMaintenance maintenance)
        {
            Maintenance = maintenance;
            return this;
        }

        public PeerMapConfiguration SetShutdownTimeout(int shutdownTimeout)
        {
            ShutdownTimeout = shutdownTimeout;
            return this;
        }

        public PeerMapConfiguration SetPeerNoVerification()
        {
            return SetPeerVerification(false);
        }

        public PeerMapConfiguration SetPeerVerification(bool peerVerification)
        {
            IsPeerVerification = peerVerification;
            return this;
        }
    }
}
