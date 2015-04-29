using TomP2P.Core.Peers;

namespace TomP2P.Dht
{
    public interface IReplicationListener
    {
        void DataInserted(Number160 locationKey);

        void DataRemoved(Number160 locationKey);
    }
}
