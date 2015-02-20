using System;
using System.Collections.Generic;
using TomP2P.Peers;
using TomP2P.Storage;

namespace TomP2P.Rpc
{
    public class DigestResult : IEquatable<DigestResult>
    {
        public SimpleBloomFilter<Number160> ContentBloomFilter { get; private set; }
        public SimpleBloomFilter<Number160> VersionBloomFilter { get; private set; }

        public SortedDictionary<Number640, ICollection<Number160>> KeyDigest { get; private set; }
        public IDictionary<Number640, Data> DataMap { get; private set; }

        public DigestResult(SimpleBloomFilter<Number160> contentBloomFilter,
            SimpleBloomFilter<Number160> versionBloomFilter)
        {
            ContentBloomFilter = contentBloomFilter;
            VersionBloomFilter = versionBloomFilter;
            KeyDigest = null;
            DataMap = null;
        }

        public DigestResult(SortedDictionary<Number640, ICollection<Number160>> keyDigest)
        {
            ContentBloomFilter = null;
            VersionBloomFilter = null;
            KeyDigest = keyDigest;
            DataMap = null;
        }

        public DigestResult(IDictionary<Number640, Data> dataMap)
        {
            ContentBloomFilter = null;
            VersionBloomFilter = null;
            KeyDigest = null;
            DataMap = dataMap;
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
            return Equals(obj as DigestResult);
        }

        public bool Equals(DigestResult other)
        {
            // TODO works?
            return Utils.Utils.Equals(KeyDigest, other.KeyDigest)
                   && Utils.Utils.Equals(ContentBloomFilter, other.ContentBloomFilter)
                   && Utils.Utils.Equals(VersionBloomFilter, other.VersionBloomFilter)
                   && Utils.Utils.Equals(DataMap, other.DataMap);
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            if (KeyDigest != null)
            {
                hashCode ^= KeyDigest.GetHashCode(); // TODO has different hash as in Java!
            }
            if (ContentBloomFilter != null)
            {
                hashCode ^= ContentBloomFilter.GetHashCode();
            }
            if (VersionBloomFilter != null)
            {
                hashCode ^= VersionBloomFilter.GetHashCode();
            }
            if (DataMap != null)
            {
                hashCode ^= DataMap.GetHashCode();
            }
            return hashCode;
        }
    }
}
