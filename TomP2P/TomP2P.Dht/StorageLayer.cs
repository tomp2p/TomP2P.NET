using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;

namespace TomP2P.Dht
{
    public class StorageLayer : IDigestStorage
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // hash of public key preferred
        private ProtectionMode _protectionDomainMode = ProtectionMode.MasterPublicKey;
        private ProtectionMode _protectionEntryMode = ProtectionMode.MasterPublicKey;
        
        // domains can generally be protected
        private ProtectionEnable _protectionDomainEnable = ProtectionEnable.All;

        // entries can generally be protected
        private ProtectionEnable _protectionEntryEnable = ProtectionEnable.All;

        // stores the domains that canno be reserved and items can be added by anyone
        private readonly ICollection<Number160> _removedDomains = new HashSet<Number160>(); 

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

    public enum ProtectionEnable
    {
        All,
        None
    }

    public enum ProtectionMode
    {
        NoMaster,
        MasterPublicKey
    }
}
