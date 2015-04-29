using System.Collections.Generic;
using TomP2P.Core.Peers;
using TomP2P.Core.Storage;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Dht
{
    /// <summary>
    /// A map that stores the values which are present in the DHT.
    /// If you plan to do transactions (put/get), make sure you do the 
    /// locking in order to not interfere with other threads that use 
    /// this map. Although the storage is threadsafe, there may be 
    /// concurrency issues with respect to transactions.
    /// </summary>
    public interface IStorage
    {
        Data Put(Number640 key, Data value);

        Data Get(Number640 key);

        bool Contains(Number640 key);

        int Contains(Number640 from, Number640 to);

        Data Remove(Number640 key, bool returnData);

        SortedDictionary<Number640, Data> Remove(Number640 from, Number640 to, bool returnData);

        SortedDictionary<Number640, Data> SubMap(Number640 from, Number640 to, int limit, bool ascending);

        SortedDictionary<Number640, Data> Map { get; }

        void Close();

        // maintenance
        void AddTimeout(Number640 key, long expiration);

        void RemoveTimeout(Number640 key);

        IEnumerable<Number640> SubMapTimeout(long to);

        int StorageCheckIntervalMillis { get; }

        // domain / entry protection
        bool ProtectDomain(Number320 key, IPublicKey publicKey);

        bool IsDomainProtectedByOthers(Number320 key, IPublicKey publicKey);

        bool ProtectEntry(Number480 key, IPublicKey publicKey);

        bool IsEntryProtectedByOthers(Number480 key, IPublicKey publicKey);

        // responsibility
        Number160 FindPeerIdsForResponsibleContent(Number160 locationKey);

        IEnumerable<Number160> FindContentForResponsiblePeerId(Number160 locationKey);

        bool UpdateResponsibilities(Number160 locationKey, Number160 peerId);

        void RemoveResponsibility(Number160 locationKey);
    }
}
