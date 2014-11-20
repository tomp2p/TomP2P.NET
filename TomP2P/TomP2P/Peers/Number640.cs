using System;
using System.Text;

namespace TomP2P.Peers
{
    /// <summary>
    /// This class stores the location, domain, content and version keys.
    /// </summary>
    public sealed class Number640 : IComparable<Number640>, IEquatable<Number640>
    {
        public static readonly Number640 Zero = new Number640(Number480.Zero, Number160.Zero);

        /// <summary>
        /// The location key.
        /// </summary>
        public Number160 LocationKey { get; private set; }

        /// <summary>
        /// The domain key.
        /// </summary>
        public Number160 DomainKey { get; private set; }

        /// <summary>
        /// The content key.
        /// </summary>
        public Number160 ContentKey { get; private set; }

        /// <summary>
        /// The version key.
        /// </summary>
        public Number160 VersionKey { get; private set; }

        /// <summary>
        /// Creates a new Number640 key from given location, domain, content and version keys.
        /// </summary>
        /// <param name="locationKey">The location key.</param>
        /// <param name="domainKey">The domain key.</param>
        /// <param name="contentKey">The content key.</param>
        /// <param name="versionKey">The version key.</param>
        public Number640(Number160 locationKey, Number160 domainKey, Number160 contentKey, Number160 versionKey)
        {
            if (locationKey == null)
            {
                throw new SystemException("LocationKey cannot be null.");
            }
            LocationKey = locationKey;
            if (domainKey == null)
            {
                throw new SystemException("DomainKey cannot be null.");
            }
            DomainKey = domainKey;
            if (contentKey == null)
            {
                throw new SystemException("ContentKey cannot be null.");
            }
            ContentKey = contentKey;
            if (versionKey == null) // TODO check if necessary
            {
                throw new SystemException("VersionKey cannot be null.");
            }
            VersionKey = versionKey;
        }
        
        /// <summary>
        /// Creates a new Number640 key from given location, domain, content and version keys.
        /// </summary>
        /// <param name="key">The location, domain and content key.</param>
        /// <param name="versionKey">The version key.</param>
        public Number640(Number480 key, Number160 versionKey)
            : this(key.LocationKey, key.DomainKey, key.ContentKey, versionKey)
        { }

        /// <summary>
        /// Creates a new Number640 key from given location, domain, content and version keys.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="contentKey"></param>
        /// <param name="versionKey"></param>
        public Number640(Number320 key, Number160 contentKey, Number160 versionKey)
            : this(key.LocationKey, key.DomainKey, contentKey, versionKey)
        { }

        /// <summary>
        /// Creates a new random Number640 key.
        /// </summary>
        /// <param name="random">The random generator.</param>
        public Number640(Random random)
            : this(new Number160(random), new Number160(random), new Number160(random), new Number160(random))
        { }

        public int CompareTo(Number640 other)
        {
            int diff = LocationKey.CompareTo(other.LocationKey);
            if (diff != 0)
            {
                return diff;
            }
            diff = DomainKey.CompareTo(other.DomainKey);
            if (diff != 0)
            {
                return diff;
            }
            diff = ContentKey.CompareTo(other.ContentKey);
            if (diff != 0)
            {
                return diff;
            }
            return VersionKey.CompareTo(other.VersionKey);
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
            return Equals(obj as Number640);
        }

        public bool Equals(Number640 other)
        {
            var t1 = LocationKey.Equals(other.LocationKey);
            var t2 = DomainKey.Equals(other.DomainKey);
            var t3 = ContentKey.Equals(other.ContentKey);
            var t4 = VersionKey.Equals(other.VersionKey);

            return t1 && t2 && t3 && t4;
        }

        public override int GetHashCode()
        {
            return LocationKey.GetHashCode() ^ DomainKey.GetHashCode() ^ ContentKey.GetHashCode() ^
                   VersionKey.GetHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            sb.Append(LocationKey).Append(",")
                .Append(DomainKey).Append(",")
                .Append(ContentKey).Append(",")
                .Append(VersionKey).Append("]");
            return sb.ToString();
        }

        public double DoubleValue
        {
            get
            {
                return (LocationKey.DoubleValue * Math.Pow(2, Number160.Bits * 3))
                    + (DomainKey.DoubleValue * Math.Pow(2, Number160.Bits * 2))
                    + (ContentKey.DoubleValue * Math.Pow(2, Number160.Bits * 1))
                    + VersionKey.DoubleValue;
            }
        }

        public float FloatValue
        {
            get { return (float) DoubleValue; }
        }

        public int IntegerValue
        {
            get { return ContentKey.IntegerValue; }
        }

        public long LongValue
        {
            get { return ContentKey.LongValue; }
        }

        public Number640 MinVersionKey
        {
            get { return new Number640(LocationKey, DomainKey, ContentKey, Number160.Zero); }
        }

        public Number640 MinContentKey
        {
            get { return new Number640(LocationKey, DomainKey, Number160.Zero, Number160.Zero); }
        }

        public Number640 MaxVersionKey
        {
            get { return new Number640(LocationKey, DomainKey, ContentKey, Number160.MaxValue); }
        }

        public Number640 MaxContentKey
        {
            get { return new Number640(LocationKey, DomainKey, Number160.MaxValue, Number160.MaxValue); }
        }

        public Number320 LocationAndDomainKey
        {
            get { return new Number320(LocationKey, DomainKey); }
        }

        public Number480 LocationAndDomainAndContentKey
        {
            get { return new Number480(LocationKey, DomainKey, ContentKey); }
        }
    }
}
