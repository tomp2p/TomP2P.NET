using System;
using System.Text;

namespace TomP2P.Core.Peers
{
    /// <summary>
    /// This class stores the location and domain key.
    /// </summary>
    public sealed class Number320 : IComparable<Number320>, IEquatable<Number320>
    {
        public static readonly Number320 Zero = new Number320(Number160.Zero, Number160.Zero);

        /// <summary>
        /// The location key.
        /// </summary>
        public Number160 LocationKey { get; private set; }

        /// <summary>
        /// The domain key.
        /// </summary>
        public Number160 DomainKey { get; private set; }

        /// <summary>
        /// Creates a new Number320 key from given location and domain keys.
        /// </summary>
        /// <param name="locationKey">The location key.</param>
        /// <param name="domainKey">The domain key.</param>
        public Number320(Number160 locationKey, Number160 domainKey)
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
        }

        public int CompareTo(Number320 other)
        {
            int diff = LocationKey.CompareTo(other.LocationKey);
            if (diff != 0)
            {
                return diff;
            }
            return DomainKey.CompareTo(other.DomainKey);
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
            return Equals(obj as Number320);
        }

        public bool Equals(Number320 other)
        {
            bool t1 = LocationKey.Equals(other.LocationKey);
            bool t2 = DomainKey.Equals(other.DomainKey);

            return t1 && t2;
        }

        public override int GetHashCode()
        {
            return LocationKey.GetHashCode() ^ DomainKey.GetHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            sb.Append(LocationKey).Append(",")
                .Append(DomainKey).Append("]");
            return sb.ToString();
        }

        public double DoubleValue
        {
            get
            {
                return (LocationKey.DoubleValue * Math.Pow(2, Number160.Bits))
                       + DomainKey.DoubleValue;
            }
        }

        public float FloatValue
        {
            get { return (float)DoubleValue; }
        }

        public int IntegerValue
        {
            get { return DomainKey.IntegerValue; }
        }

        public long LongValue
        {
            get { return DomainKey.LongValue; }
        }

        /// <summary>
        /// The minimum value of a content key.
        /// </summary>
        public Number480 MinContentKey // TODO check necessity
        {
            get { return new Number480(LocationKey, DomainKey, Number160.Zero); }
        }

        /// <summary>
        /// The maximum value of a content key.
        /// </summary>
        public Number480 MaxContentKey // TODO check necessity
        {
            get {  return new Number480(LocationKey, DomainKey, Number160.MaxValue); }
        }
    }
}
