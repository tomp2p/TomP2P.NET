using System;
using System.Text;

namespace TomP2P.Core.Peers
{
    /// <summary>
    /// This class stores the location, domain and content key.
    /// </summary>
    public sealed class Number480 : IComparable<Number480>, IEquatable<Number480>
    {
        public static readonly Number480 Zero = new Number480(Number320.Zero, Number160.Zero);

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
        /// Creates a new Number480 key from given location, domain and content keys.
        /// </summary>
        /// <param name="locationKey">The location key.</param>
        /// <param name="domainKey">The domain key.</param>
        /// <param name="contentKey">The content key.</param>
        public Number480(Number160 locationKey, Number160 domainKey, Number160 contentKey)
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
        }

        /// <summary>
        /// Creates a new Number480 key from given location, domain and content keys.
        /// </summary>
        /// <param name="key">The location and domain key.</param>
        /// <param name="contentKey">The content key.</param>
        public Number480(Number320 key, Number160 contentKey)
            : this(key.LocationKey, key.DomainKey, contentKey)
        { }

        public Number480(Random random)
            : this(new Number160(random), new Number160(random), new Number160(random))
        { }

        public int CompareTo(Number480 other)
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
            return ContentKey.CompareTo(other.ContentKey);
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
            return Equals(obj as Number480);
        }

        public bool Equals(Number480 other)
        {
            var t1 = LocationKey.Equals(other.LocationKey);
            var t2 = DomainKey.Equals(other.DomainKey);
            var t3 = ContentKey.Equals(other.ContentKey);

            return t1 && t2 && t3;
        }

        public override int GetHashCode()
        {
            return LocationKey.GetHashCode() ^ DomainKey.GetHashCode() ^ ContentKey.GetHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            sb.Append(LocationKey).Append(",")
                .Append(DomainKey).Append(",")
                .Append(ContentKey).Append("]");
            return sb.ToString();
        }

        public double DoubleValue
        {
            get
            {
                return (LocationKey.DoubleValue*Math.Pow(2, Number160.Bits*2))
                       + (DomainKey.DoubleValue*Math.Pow(2, Number160.Bits))
                       + ContentKey.DoubleValue;
            }
        }

        public float FloatValue
        {
            get { return (float)DoubleValue; }
        }

        public int IntegerValue
        {
            get { return ContentKey.IntegerValue; }
        }

        public long LongValue
        {
            get { return ContentKey.LongValue; }
        }
    }
}
