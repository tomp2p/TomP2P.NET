using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;

namespace TomP2P.Dht
{
    public enum PutStatus
    {
        Ok,
        OkPrepared,
        OkUnchanged,
        FailedNotAbsent,
        FailedSecurity,
        Failed,
        VersionFork,
        NotFound,
        Deleted
    }

    public class StorageLayer : IDigestStorage
    {


        public DigestInfo Digest(Number640 from, Number640 to, int limit, bool ascending)
        {
            throw new NotImplementedException();
        }

        public DigestInfo Digest(Number320 locationAndDomainKey, SimpleBloomFilter<Number160> keyBloomFilter, SimpleBloomFilter<Number160> contentBloomFilter, int limit, bool ascending, bool isBloomFilterAnd)
        {
            throw new NotImplementedException();
        }

        public DigestInfo Digest(ICollection<Core.Peers.Number640> number640Collection)
        {
            throw new NotImplementedException();
        }
    }
}
