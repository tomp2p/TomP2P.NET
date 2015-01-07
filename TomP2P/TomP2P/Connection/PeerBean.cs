using System.Collections.Concurrent;
using System.Collections.Generic;
using NLog;
using TomP2P.Extensions.Workaround;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    // TODO finish implementation of PeerBean

    /// <summary>
    /// A bean that holds non-sharable (unique for each peer) configuration settings for the peer.
    /// The sharable configurations are stored in a <see cref="ConnectionBean"/>.
    /// </summary>
    public class PeerBean
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// The key pair that holds private and public key.
        /// </summary>
        public KeyPair KeyPair { get; private set; }
        /// <summary>
        /// The address of this peer. This address may change.
        /// </summary>
        public PeerAddress ServerPeerAddress { get; private set; }
        /// <summary>
        /// The map that stores neighbors.
        /// </summary>
        public PeerMap PeerMap { get; private set; }

        /// <summary>
        /// The listeners that are interested in the peer's status.
        /// E.g., peer is found to be online, offline or failed to respond in time.
        /// </summary>
        public IList<IPeerStatusListener> PeerStatusListeners { get; private set; }
        public IBloomfilterFactory BloomfilterFactory { get; private set; }
        public MaintenanceTask MaintenanceTask { get; private set; }
        public IDigestStorage DigestStorage { get; private set; }
        public IDigestTracker DigestTracker { get; private set; }

        /// <summary>
        /// This dictionary is used for all currently open PeerConnections which are meant to stay open.
        /// Number160 = peer ID.
        /// </summary>
        public ConcurrentDictionary<Number160, PeerConnection> OpenPeerConnections { get; private set; }

        /// <summary>
        /// Creates a peer bean with a key pair.
        /// </summary>
        /// <param name="keyPair">The key pair that holds private and public key.</param>
        public PeerBean(KeyPair keyPair)
        {
            KeyPair = keyPair;
            PeerStatusListeners = new List<IPeerStatusListener>(1);
            OpenPeerConnections = new ConcurrentDictionary<Number160, PeerConnection>();
        }

        /// <summary>
        /// Sets a new key pair for this peer.
        /// </summary>
        /// <param name="keyPair">The new private and public key for this peer.</param>
        /// <returns>This class.</returns>
        public PeerBean SetKeyPair(KeyPair keyPair)
        {
            KeyPair = keyPair;
            return this;
        }

        /// <summary>
        /// Sets a new address for this peer.
        /// </summary>
        /// <param name="serverPeerAddress">The new address of this peer.</param>
        /// <returns>This class.</returns>
        public PeerBean SetServerPeerAddress(PeerAddress serverPeerAddress)
        {
            ServerPeerAddress = serverPeerAddress;
            return this;
        }

        /// <summary>
        /// Sets a new map storing neighbors for this peer.
        /// </summary>
        /// <param name="peerMap">The new map that stores neighbors.</param>
        /// <returns>This class.</returns>
        public PeerBean SetPeerMap(PeerMap peerMap)
        {
            PeerMap = peerMap;
            return this;
        }

        /// <summary>
        /// Adds a PeerStatusListener to this peer.
        /// </summary>
        /// <param name="peerStatusListener">The new listener that is interested in the peer's status.</param>
        /// <returns>This class.</returns>
        public PeerBean AddPeerStatusListener(IPeerStatusListener peerStatusListener)
        {
            lock (PeerStatusListeners)
            {
                PeerStatusListeners.Add(peerStatusListener);
            }
            return this;
        }

        /// <summary>
        /// Removes a PeerStatusListener from this peer.
        /// </summary>
        /// <param name="peerStatusListener">The listener that is no longer intereseted in the peer's status.</param>
        /// <returns>This class.</returns>
        public PeerBean RemovePeerStatusListener(IPeerStatusListener peerStatusListener)
        {
            lock (PeerStatusListeners)
            {
                PeerStatusListeners.Remove(peerStatusListener);
            }
            return this;
        }

        public PeerBean SetBloomfilterFactory(IBloomfilterFactory bloomfilterFactory)
        {
            BloomfilterFactory = bloomfilterFactory;
            return this;
        }

        public PeerBean SetMaintenanceTask(MaintenanceTask maintencanceTask)
        {
            MaintenanceTask = maintencanceTask;
            return this;
        }

        public PeerBean SetDigestStorage(DigestStorage digestStorage)
        {
            DigestStorage = digestStorage;
            return this;
        }

        public PeerBean SetDigestTracker(DigestTracker digestTracker)
        {
            DigestTracker = digestTracker;
            return this;
        }

        /// <summary>
        /// Returns the PeerConnection for the given Number160 peer ID.
        /// </summary>
        /// <param name="peerId">The ID of the peer.</param>
        /// <returns>The connection associated to the peer ID.</returns>
        public PeerConnection PeerConnection(Number160 peerId)
        {
            PeerConnection peerConnection = OpenPeerConnections[peerId];
            if (peerConnection != null)
            {
                return peerConnection;
            }
            else
            {
                Logger.Error("There was no PeerConnection for peer ID = " + peerId);
                return null;
            }
        }
    }
}
