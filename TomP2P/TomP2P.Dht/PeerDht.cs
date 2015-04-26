using System.Threading.Tasks;
using TomP2P.Core.Connection;
using TomP2P.Core.P2P;
using TomP2P.Core.Peers;

namespace TomP2P.Dht
{
    public class PeerDht
    {
        private Peer Peer { get; private set; }
        private StorageRpc StorageRpc { get; private set; }
        private DistributedHashTable Dht { get; private set; }
        private StorageLayer StorageLayer { get; private set; }

        internal PeerDht(Peer peer, StorageLayer storageLayer, DistributedHashTable dht, StorageRpc storageRpc)
        {
            Peer = peer;
            StorageLayer = storageLayer;
            Dht = dht;
            StorageRpc = storageRpc;
        }

        public AddBuilder Add(Number160 locationKey)
        {
            return new AddBuilder(this, locationKey);
        }

        public PutBuilder Put(Number160 locationKey)
        {
            return new PutBuilder(this, locationKey);
        }

        public GetBuilder Get(Number160 locationKey)
        {
            return new GetBuilder(this, locationKey);
        }

        public DigestBuilder Digest(Number160 locationKey)
        {
            return new DigestBuilder(this, locationKey);
        }

        public RemoveBuilder Remove(Number160 locationKey)
        {
            return new RemoveBuilder(this, locationKey);
        }

        /// <summary>
        /// The send method works as follows:
        /// 1. Routing: find close peers to the content hash.
        /// You can control the routing behavior with SetRoutingConfiguration().
        /// 2. Sending: send the data to the N closest peers. N is set via SetRequestP2PConfiguration().
        /// If you want to send it to the closest one, use (1, 5, 0) as parameters.
        /// </summary>
        /// <param name="locationKey"></param>
        /// <returns></returns>
        public SendBuilder Send(Number160 locationKey)
        {
            return new SendBuilder(this, locationKey);
        }

        public ParallelRequestBuilder<T> ParallelRequest(Number160 locationKey)
        {
            return new ParallelRequestBuilder<FutureDht<T>>(this, locationKey);
        }

        // convenience methods
        public Task ShutdownAsync()
        {
            return Peer.ShutdownAsync();
        }

        public PeerBean PeerBean
        {
            get { return Peer.PeerBean; }
        }

        public Number160 PeerId
        {
            get { return Peer.PeerId; }
        }

        public PeerAddress PeerAddress
        {
            get { return Peer.PeerAddress; }
        }
    }
}
