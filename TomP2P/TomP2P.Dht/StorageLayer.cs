using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NLog;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;
using TomP2P.Core.Utils;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

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

        public IDictionary<Number640, Enum> PutAll(SortedDictionary<Number640, Data> dataMap, IPublicKey publicKey, bool putIfAbsent, bool domainProtection,
            bool sendSelf)
        {
            if (dataMap.Count == 0)
            {
                return Convenient.EmptyDictionary<Number640, Enum>();
            }
            var min = dataMap.First().Key; // TODO check if correct
            var max = dataMap.Last().Key;
            var retVal = new Dictionary<Number640, Enum>();
            var keysToCheck = new HashSet<Number480>();
            var rangeLock = Lock(min, max);
            try
            {
                foreach (var kvp in dataMap)
                {
                    var key = kvp.Key;
                    keysToCheck.Add(key.LocationAndDomainAndContentKey);
                    var newData = kvp.Value;
                    if (!SecurityDomainCheck(key.LocationAndDomainKey, publicKey, publicKey, domainProtection))
                    {
                        retVal.Add(key, PutStatus.FailedSecurity);
                        continue;
                    }

                    // We need this check in case we did not use the encoder/deconder,
				    // which is the case if we send the message to ourself. In that
				    // case, the public key of the data is never set to the message
				    // publick key, if the publick key of the data was null.
                    IPublicKey dataKey;
                    if (sendSelf && newData.PublicKey == null)
                    {
                        dataKey = publicKey;
                    }
                    else
                    {
                        dataKey = newData.PublicKey;
                    }

                    if (!SecurityEntryCheck(key.LocationAndDomainAndContentKey, publicKey, dataKey, newData.IsProtectedEntry))
                    {
					    retVal.Add(key, PutStatus.FailedSecurity);
					    continue;
				    }

                    var contains = _backend.Contains(key);
                    if (contains)
                    {
					    if(putIfAbsent)
                        {
						    retVal.Add(key, PutStatus.FailedNotAbsent);
						    continue;
					    }
                        var oldData = _backend.Get(key);
					    if(oldData.IsDeleted)
                        {
						    retVal.Add(key, PutStatus.Deleted);
						    continue;
					    }
					    if(!oldData.BasedOnSet.Equals(newData.BasedOnSet))
                        {
						    retVal.Add(key, PutStatus.VersionFork);
						    continue;
					    }
				    }

                    var oldData2 = _backend.Put(key, newData);
				    long expiration = newData.ExpirationMillis;
				    // handle timeout
				    _backend.AddTimeout(key, expiration);
				    if(newData.HasPrepareFlag)
                    {
					    retVal.Add(key, PutStatus.OkPrepared);
				    }
                    else
                    {
					    if(newData.Equals(oldData2))
                        {
						    retVal.Add(key, PutStatus.OkUnchanged);
					    }
                        else
                        {
						    retVal.Add(key, PutStatus.Ok);
					    }
				    }
                }

                //now check for forks
			    foreach (var key in keysToCheck)
                {
				    var minVersion = new Number640(key, Number160.Zero);
				    var maxVersion = new Number640(key, Number160.MaxValue);
				    var tmp = _backend.SubMap(minVersion, maxVersion, -1, true);
				    var heads = GetLatestInternal(tmp);
				    if(heads.Count > 1)
                    {
					    foreach (var fork in heads.Keys)
                        {
						    if(retVal.ContainsKey(fork))
                            {
							    retVal.Add(fork, PutStatus.VersionFork);
						    }
					    }
				    }
			    }
			    return retVal;
            }
            finally
            {
                rangeLock.Unlock();
            }
        }

        public Enum Put(Number640 key, Data newData, IPublicKey publicKey, bool putIfAbsent, bool domainProtection,
            bool sendSelf)
        {
            var dataMap = new SortedDictionary<Number640, Data>();
		    dataMap.Add(key, newData);
		    var putStatus = PutAll(dataMap, publicKey, putIfAbsent, domainProtection, sendSelf);
		    var retVal = putStatus[key];
		    if(retVal == null)
            {
			    return PutStatus.Failed;
		    }
            return retVal;
        }

        public Pair<Data, Enum> Remove(Number640 key, IPublicKey publicKey, bool returnData)
        {
		    var rangeLock = Lock(key);
		    try 
            {
			    if (!CanClaimDomain(key.LocationAndDomainKey, publicKey))
                {
				    return new Pair<Data, Enum>(null, PutStatus.FailedSecurity);
			    }
			    if (!CanClaimEntry(key.LocationAndDomainAndContentKey, publicKey))
                {
				    return new Pair<Data, Enum>(null, PutStatus.FailedSecurity);
			    }
			    if (!_backend.Contains(key))
                {
				    return new Pair<Data, Enum>(null, PutStatus.NotFound);
			    }
			    _backend.RemoveTimeout(key);
			    return new Pair<Data, Enum>(_backend.Remove(key, returnData), PutStatus.Ok);
		    } 
            finally
            {
			    rangeLock.Unlock();
		    }
	    }

        public Data Get(Number640 key)
        {
		    var rangeLock = Lock(key);
		    try
            {
			    return GetInternal(key);
		    }
            finally
            {
			    rangeLock.Unlock();
		    }
	    }

        private Data GetInternal(Number640 key)
        {
		    var data = _backend.Get(key);
		    if (data != null && !data.HasPrepareFlag)
            {
			    return data;
		    }
            return null;
        }

        public SortedDictionary<Number640, Data> Get(Number640 from, Number640 to, int limit, bool ascending)
        {
		    var rLock = _rangeLock.Lock(from, to);
		    try
            {
			    var tmp = _backend.SubMap(from, to, limit, ascending);
			    RemovePrepared(tmp);

			    return tmp;
		    }
            finally
            {
			    rLock.Unlock();
		    }
	    }

        public SortedDictionary<Number640, Data> GetLatestVersion(Number640 key)
        {
		    var rangeLock = Lock(key.LocationAndDomainAndContentKey);
		    try
            {
			    var tmp = _backend.SubMap(key.MinVersionKey, key.MaxVersionKey, -1, true);
			    RemovePrepared(tmp);
			    return GetLatestInternal(tmp);
		    }
            finally
            {
			    rangeLock.Unlock();
		    }
	    }

        private static SortedDictionary<Number640, Data> GetLatestInternal(SortedDictionary<Number640, Data> tmp)
        {
	        // delete all predecessors
		    var result = new SortedDictionary<Number640, Data>();
            while (tmp.Count != 0)
            {
	    	    // first entry is a latest version
	    	    var latest = tmp.Last(); // TODO check if correct
	    	    // store in results list
	    	    result.Add(latest.Key, latest.Value);
	    	    // delete all predecessors of latest entry
	    	    DeletePredecessors(latest.Key, tmp);
	        }
	        return result;
        }

        private static void RemovePrepared(IDictionary<Number640, Data> tmp)
        {
            // TODO check if correct
            foreach (var kvp in tmp.ToList()) // iterate over copy
            {
                if (kvp.Value.HasPrepareFlag)
                {
                    tmp.Remove(kvp.Key); // delete from original
                }
            }
        }

        private static void DeletePredecessors(Number640 key, IDictionary<Number640, Data> sortedMap)
        {
		    var toRemove = new List<Number640>();
		    toRemove.Add(key);
		    
            while(toRemove.Count != 0)
            {
			    var version = sortedMap.Remove2(toRemove.RemoveAt2(0));
			    // check if version has been already deleted
                // check if version is initial version
			    if (version != null && version.BasedOnSet.Count != 0)
                {
				    foreach (var basedOnKey in version.BasedOnSet)
                    {
					    toRemove.Add(new Number640(key.LocationAndDomainAndContentKey, basedOnKey));
				    }
			    }
		    }
	    }

        public SortedDictionary<Number640, Data> Get()
        {
		    var rangeLock = Lock();
		    try
            {
			    return _backend.Map;
		    }
            finally
            {
			    rangeLock.Unlock();
		    }
	    }

        public bool Contains(Number640 key)
        {
		    var rangeLock = Lock(key);
		    try
		    {
		        return _backend.Contains(key);
		    }
            finally
            {
			    rangeLock.Unlock();
		    }
	    }

        public SortedDictionary<Number640, Data> Get(Number640 from, Number640 to, SimpleBloomFilter<Number160> contentKeyBloomFilter,
	        SimpleBloomFilter<Number160> versionKeyBloomFilter, SimpleBloomFilter<Number160> contentBloomFilter,  int limit, 
            bool ascending, bool isBloomFilterAnd)
        {
		    var rLock = _rangeLock.Lock(from, to);
		    try
            {
			    var tmp = _backend.SubMap(from, to, limit, ascending);

                foreach (var kvp in tmp.ToList()) // iterate over copy
                {
                    // remove from original
                    if (kvp.Value.HasPrepareFlag)
                    {
                        tmp.Remove(kvp.Key);
                        continue;
                    }
                    if (isBloomFilterAnd)
                    {
                        if (!contentKeyBloomFilter.Contains(kvp.Key.ContentKey))
                        {
                            tmp.Remove(kvp.Key);
                            continue;
                        }
                        if (!versionKeyBloomFilter.Contains(kvp.Key.VersionKey))
                        {
                            tmp.Remove(kvp.Key);
                            continue;
                        }
                        if (!contentBloomFilter.Contains(kvp.Value.Hash))
                        {
                            tmp.Remove(kvp.Key);
                        }
                    }
                    else
                    {
                        if (contentKeyBloomFilter.Contains(kvp.Key.ContentKey))
                        {
						    tmp.Remove(kvp.Key);
						    continue;
					    }
					    if (versionKeyBloomFilter.Contains(kvp.Key.VersionKey))
                        {
						    tmp.Remove(kvp.Key);
						    continue;
					    }
					    if (contentBloomFilter.Contains(kvp.Value.Hash)) 
                        {
						    tmp.Remove(kvp.Key);
					    }
                    }
                }
			    return tmp;
		    }
            finally
            {
			    rLock.Unlock();
		    }
	    }

        public SortedDictionary<Number640, Data> RemoveReturnData(Number640 from, Number640 to, IPublicKey publicKey)
        {
		    var rLock = _rangeLock.Lock(from, to);
		    try
            {
			    var tmp = _backend.SubMap(from, to, -1, true);

			    foreach (var key in tmp.Keys)
                {
				    // fail fast, as soon as we want to remove 1 domain that we cannot, abort
				    if (!CanClaimDomain(key.LocationAndDomainKey, publicKey))
                    {
					    return null;
				    }
				    if (!CanClaimEntry(key.LocationAndDomainAndContentKey, publicKey))
                    {
					    return null;
				    }
			    }
			    var result = _backend.Remove(from, to, true);
			    foreach (var kvp in result)
                {
				    var data = kvp.Value;
				    if (data.PublicKey == null || data.PublicKey.Equals(publicKey))
                    {
					    _backend.RemoveTimeout(kvp.Key);
				    }
			    }
			    return result;
		    }
            finally
            {
			    rLock.Unlock();
		    }
	    }

        public SortedDictionary<Number640, byte> RemoveReturnStatus(Number640 from, Number640 to, IPublicKey publicKey)
        {
		    var rLock = _rangeLock.Lock(from, to);
		    try
            {
			    var tmp = _backend.SubMap(from, to, -1, true);
			    var result = new SortedDictionary<Number640, byte>();
			    foreach (var key in tmp.Keys)
                {
				    var pair = Remove(key, publicKey, false);
				    result.Put(key, (byte) Convert.ToInt32(pair.Element1)); // TODO check if works
			    }
			    return result;
		    }
            finally
            {
			    rLock.Unlock();
		    }
	    }

        public void CheckTimeout()
        {
		    var time = Convenient.CurrentTimeMillis();
		    var toRemove = _backend.SubMapTimeout(time);
		    foreach (var key in toRemove)
            {
			    var rangeLock = Lock(key);
			    try
                {
				    _backend.Remove(key, false);
				    _backend.RemoveTimeout(key);
				    // remove responsibility if we don't have any data stored under locationkey
				    var locationKey = key.LocationKey;
				    var lockResp= LockResponsibility(locationKey);
				    try
                    {
					    if (IsEmpty(locationKey))
                        {
						    _backend.RemoveResponsibility(locationKey);
					    }
				    }
                    finally
                    {
					    lockResp.Unlock();
				    }
			    }
                finally
                {
				    rangeLock.Unlock();
			    }
		    }
	    }

        private bool IsEmpty(Number160 locationKey)
        {
		    var from = new Number640(locationKey, Number160.Zero, Number160.Zero, Number160.Zero);
		    var to = new Number640(locationKey, Number160.MaxValue, Number160.MaxValue, Number160.MaxValue);
		    var tmp = _backend.SubMap(from, to, 1, false);
		    return tmp.Count == 0;
	    }

        public DigestInfo Digest(Number640 from, Number640 to, int limit, bool ascending)
        {
            var digestInfo = new DigestInfo();
		    var rLock = _rangeLock.Lock(from, to);
		    try
            {
			    var tmp = _backend.SubMap(from, to, limit, ascending);
			    foreach (var kvp in tmp)
                {
				    if (!kvp.Value.HasPrepareFlag)
                    {
					    digestInfo.Put(kvp.Key, kvp.Value.BasedOnSet);
				    }
			    }
			    return digestInfo;
		    }
            finally
            {
			    rLock.Unlock();
		    }
        }

        public DigestInfo Digest(Number320 locationAndDomainKey, SimpleBloomFilter<Number160> keyBloomFilter, SimpleBloomFilter<Number160> contentBloomFilter, int limit, bool ascending, bool isBloomFilterAnd)
        {
            var digestInfo = new DigestInfo();
		    var rLock = Lock(locationAndDomainKey);
		    try {
			    var from = new Number640(locationAndDomainKey, Number160.Zero, Number160.Zero);
			    var to = new Number640(locationAndDomainKey, Number160.MaxValue, Number160.MaxValue);
			    var tmp = _backend.SubMap(from, to, limit, ascending);

			    foreach (var kvp in tmp)
                {
				    if (isBloomFilterAnd)
                    {
					    if (keyBloomFilter == null || keyBloomFilter.Contains(kvp.Key.ContentKey))
                        {
						    if (contentBloomFilter == null || contentBloomFilter.Contains(kvp.Value.Hash))
                            {
							    if (!kvp.Value.HasPrepareFlag)
                                {
								    digestInfo.Put(kvp.Key, kvp.Value.BasedOnSet);
							    }
						    }
					    }
				    }
                    else
                    {
					    if (keyBloomFilter == null || !keyBloomFilter.Contains(kvp.Key.ContentKey))
                        {
						    if (contentBloomFilter == null || !contentBloomFilter.Contains(kvp.Value.Hash))
                            {
							    if (!kvp.Value.HasPrepareFlag)
                                {
								    digestInfo.Put(kvp.Key, kvp.Value.BasedOnSet);
							    }
						    }
					    }
				    }
			    }
			    return digestInfo;
		    } 
            finally
            {
			    rLock.Unlock();
		    }
        }

        public DigestInfo Digest(ICollection<Number640> number640Collection)
        {
            var digestInfo = new DigestInfo();
		    foreach (var num640 in number640Collection)
            {
			    var rangeLock = Lock(num640);
			    try
                {
				    if (_backend.Contains(num640))
                    {
					    var data = GetInternal(num640);
					    if (data != null)
                        {
						    digestInfo.Put(num640, data.BasedOnSet);
					    }
				    }
			    }
                finally
                {
				    rangeLock.Unlock();
			    }
		    }
		    return digestInfo;
        }

        private bool SecurityDomainCheck(Number320 key, IPublicKey publicKey, IPublicKey newPublicKey, bool domainProtection) 
        {
		    var domainProtectedByOthers = _backend.IsDomainProtectedByOthers(key, publicKey);
			Logger.Debug("No domain protection requested {0} for domain {1}.", Utils.Hash(newPublicKey), key);
		    // I dont want to claim the domain
		    if (!domainProtection)
            {
                // Returns true if the domain is not protceted by others, otherwise
			    // false if the domain is protected
			    return !domainProtectedByOthers;
		    }
            if (CanClaimDomain(key, publicKey))
            {
                if (CanProtectDomain(key.DomainKey, publicKey))
                {
                    Logger.Debug("Set domain protection.");
                    return _backend.ProtectDomain(key, newPublicKey);
                }
                return true;
            }
            return false;
	    }

        private bool SecurityEntryCheck(Number480 key, IPublicKey publicKeyMessage, IPublicKey publicKeyData, bool entryProtection) 
        {
		    var entryProtectedByOthers = _backend.IsEntryProtectedByOthers(key, publicKeyMessage);
		    // I dont want to claim the domain
		    if (!entryProtection)
            {
			    // Returns true if the domain is not protceted by others, otherwise
			    // false if the domain is protected
			    return !entryProtectedByOthers;
		    }
            //replication cannot sign messages with the originators key, so we must also check the public key of the data
            if (CanClaimEntry(key, publicKeyMessage) || CanClaimEntry(key, publicKeyData))
            {
                if (CanProtectEntry(key.DomainKey, publicKeyMessage))
                {
                    return _backend.ProtectEntry(key, publicKeyData);
                }
                return true;
            }
            return false;
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
