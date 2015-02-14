using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.Storage
{
    public interface IDigestTracker
    {
        DigestInfo Digest(Number160 locationKey, Number160 domainKey, Number160 contentKey);
    }
}
