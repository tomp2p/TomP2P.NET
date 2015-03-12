using System;
using System.Collections.Generic;
using TomP2P.Core.Peers;

namespace TomP2P.Core.Rpc
{
    /// <summary>
    /// Calculates or sets a global hash. The digest is used in two places:
    /// - For routing, where a message needs to have a predictable size. Thus in
    ///   this case a global hash is calculated.
    /// - Get() for getting a list of hashes from peers. Here we don't need to restrict
    ///   ourself, since we use TCP.
    /// </summary>
    public class DigestInfo : IEquatable<DigestInfo>
    {
        private volatile Number160 _keyDigest = null;
        private volatile Number160 _contentDigest = null;

        private volatile int _size = -1;

        private readonly SortedDictionary<Number640, ICollection<Number160>> _mapDigests = new SortedDictionary<Number640, ICollection<Number160>>();

        /// <summary>
        /// Empty constructor is used to add the hashes to the list.
        /// </summary>
        public DigestInfo()
        { }

        /// <summary>
        /// Creates a digest with the size only.
        /// </summary>
        /// <param name="size"></param>
        public DigestInfo(int size)
        {
            _size = size;
        }

        /// <summary>
        /// If a global hash has already been calculated, then this constructor is udes to
        /// store those. Note that once a global hash is set it cannot be unset.
        /// </summary>
        /// <param name="keyDigest">The digest of all keys.</param>
        /// <param name="contentDigest">The digest of all contents.</param>
        /// <param name="size">The number of entries.</param>
        public DigestInfo(Number160 keyDigest, Number160 contentDigest, int size)
        {
            _keyDigest = keyDigest;
            _contentDigest = contentDigest;
            _size = size;
        }

        /// <summary>
        /// Returns or calculates the global key hash. The global key hash will be calculated
        /// if the empty constructor is used.
        /// </summary>
        public Number160 KeyDigest
        {
            get
            {
                if (_keyDigest == null)
                {
                    Process();
                }
                return _keyDigest;
            }
        }

        /// <summary>
        /// Returns or calculates the global content hash. The global content hash will be calculated
        /// if the empty constructor is used.
        /// </summary>
        public Number160 ContentDigest
        {
            get
            {
                if (_contentDigest == null)
                {
                    Process();
                }
                return _contentDigest;
            }
        }

        /// <summary>
        /// Calculates the digest.
        /// </summary>
        private void Process()
        {
            var hashKey = Number160.Zero;
            var hashContent = Number160.Zero;
            foreach (var entry in _mapDigests)
            {
                hashKey = hashKey.Xor(entry.Key.LocationKey);
                hashKey = hashKey.Xor(entry.Key.DomainKey);
                hashKey = hashKey.Xor(entry.Key.ContentKey);
                hashKey = hashKey.Xor(entry.Key.VersionKey);
                foreach (var basedOn in entry.Value)
                {
                    hashContent = hashContent.Xor(basedOn);
                }
            }
            _keyDigest = hashKey;
            _contentDigest = hashContent;
        }

        public SimpleBloomFilter<Number160> LocationKeyBloomFilter(IBloomfilterFactory factory)
        {
            var sbf = factory.CreateLocationKeyBloomFilter();
            foreach (var entry in _mapDigests)
            {
                sbf.Add(entry.Key.LocationKey);
            }
            return sbf;
        }

        public SimpleBloomFilter<Number160> DomainKeyBloomFilter(IBloomfilterFactory factory)
        {
            var sbf = factory.CreateDomainKeyBloomFilter();
            foreach (var entry in _mapDigests)
            {
                sbf.Add(entry.Key.DomainKey);
            }
            return sbf;
        }

        public SimpleBloomFilter<Number160> ContentKeyBloomFilter(IBloomfilterFactory factory)
        {
            var sbf = factory.CreateContentKeyBloomFilter();
            foreach (var entry in _mapDigests)
            {
                sbf.Add(entry.Key.ContentKey);
            }
            return sbf;
        }

        public SimpleBloomFilter<Number160> VersionKeyBloomFilter(IBloomfilterFactory factory)
        {
            var sbf = factory.CreateContentBloomFilter();
            foreach (var entry in _mapDigests)
            {
                sbf.Add(entry.Key.VersionKey);
            }
            return sbf;
        }

        /// <summary>
        /// Stores a key and the hash of the content for further processing.
        /// </summary>
        /// <param name="key">The key of the content.</param>
        /// <param name="basedOnSet">The hash of the content.</param>
        public void Put(Number640 key, ICollection<Number160> basedOnSet)
        {
            _mapDigests.Add(key, basedOnSet);
        }

        /// <summary>
        /// The list of hashes.
        /// </summary>
        public SortedDictionary<Number640, ICollection<Number160>> Digests
        {
            get { return _mapDigests; }
        }

        /// <summary>
        /// Thes number of hashes.
        /// </summary>
        public int Size
        {
            get
            {
                if (_size == -1)
                {
                    _size = _mapDigests.Count;
                }
                return _size;
            }
        }

        /// <summary>
        /// True, if the digest information has not been provided.
        /// </summary>
        public bool IsEmpty
        {
            get { return _size <= 0; }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as DigestInfo);
        }

        public bool Equals(DigestInfo other)
        {
            return KeyDigest.Equals(other.KeyDigest) 
                && Size == other.Size 
                && ContentDigest.Equals(other.ContentDigest);
        }

        public override int GetHashCode()
        {
            return KeyDigest.GetHashCode() ^ Size ^ ContentDigest.GetHashCode();
        }
    }
}
