using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;

namespace TomP2P.Core.Storage
{
    public interface IDigestTracker
    {
        DigestInfo Digest(Number160 locationKey, Number160 domainKey, Number160 contentKey);
    }
}
