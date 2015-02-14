using System.Collections.Generic;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.Storage
{
    public interface IDigestStorage
    {
        DigestInfo Digest(Number640 from, Number640 to, int limit, bool ascending);

        DigestInfo Digest(Number320 locationAndDomainKey, SimpleBloomFilter<Number160> keyBloomFilter,
            SimpleBloomFilter<Number160> contentBloomFilter, int limit, bool ascending, bool isBloomFilterAnd);

        DigestInfo Digest(ICollection<Number640> number640Collection);
    }
}
