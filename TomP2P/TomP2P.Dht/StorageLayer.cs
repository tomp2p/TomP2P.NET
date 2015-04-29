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
        public ProtectionMode ProtectionDomainMode { get; private set; } = ProtectionMode.MasterPublicKey;
        public ProtectionMode ProtectionEntryMode { get; private set; } = ProtectionMode.MasterPublicKey;
        
        // domains can generally be protected
        public ProtectionEnable ProtectionDomainEnable { get; private set; } = ProtectionEnable.All;

        // entries can generally be protected
        public ProtectionEnable ProtectionEntryEnable { get; private set; } = ProtectionEnable.All;

        // stores the domains that canno be reserved and items can be added by anyone
        private readonly ICollection<Number160> _removedDomains = new HashSet<Number160>(); 

        private readonly RangeLock<Number640> _rangeLock = new RangeLock<Number640>();
        private readonly RangeLock<Number640> _responsibilityLock = new RangeLock<Number640>();

        private readonly IStorage _backend;

        public StorageLayer(IStorage backend)
        {
            _backend = backend;
        }

        public void SetProtection(ProtectionEnable protectionDomainEnable, ProtectionMode protectionDomainMode,
            ProtectionEnable protectionEntryEnable, ProtectionMode protectionEntryMode)
        {
            ProtectionDomainEnable = protectionDomainEnable;
            ProtectionDomainMode = protectionDomainMode;
            ProtectionEntryEnable = protectionEntryEnable;
            ProtectionEntryMode = protectionEntryMode;
        }

        public void RemoveDomainProtection(Number160 domain)
        {
            _removedDomains.Add(domain);
        }

        public bool IsDomainRemoved(Number160 domain)
        {
            return _removedDomains.Contains(domain);
        }

        private RangeLock<Number640>.Range Lock(Number640 min, Number640 max)
        {
            return _rangeLock.Lock(min, max);
        }

        private RangeLock<Number640>.Range Lock(Number640 number640)
        {
            return _rangeLock.Lock(number640, number640);
        }

        private RangeLock<Number640>.Range Lock(Number480 number480)
        {
            return _rangeLock.Lock(
                new Number640(number480, Number160.Zero), 
                new Number640(number480, Number160.MaxValue));
        }

        private RangeLock<Number640>.Range Lock(Number320 number320)
        {
            return _rangeLock.Lock(
                new Number640(number320, Number160.Zero, Number160.Zero), 
                new Number640(number320, Number160.MaxValue, Number160.MaxValue));
        }

        private RangeLock<Number640>.Range Lock(Number160 number160)
        {
            return _rangeLock.Lock(
                new Number640(number160, Number160.Zero, Number160.Zero, Number160.Zero), 
                new Number640(number160, Number160.MaxValue, Number160.MaxValue, Number160.MaxValue));
        }

        private RangeLock<Number640>.Range LockResponsibility(Number160 number160)
        {
            return _responsibilityLock.Lock(
                new Number640(number160, Number160.Zero, Number160.Zero, Number160.Zero), 
                new Number640(number160, Number160.MaxValue, Number160.MaxValue, Number160.MaxValue));
        }

        private RangeLock<Number640>.Range Lock()
        {
            return _rangeLock.Lock(
                new Number640(Number160.Zero, Number160.Zero, Number160.Zero, Number160.Zero),
                new Number640(Number160.MaxValue, Number160.MaxValue, Number160.MaxValue, Number160.MaxValue));
        }



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
