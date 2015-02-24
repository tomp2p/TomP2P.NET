using System.Globalization;
using System.Threading;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// An attempt to mimick Java's AtomicLong in .NET.
    /// In .NET, however, it is reasonable to make it a struct rather than a class.
    /// This is also used instead of Java's "volatile long", because long uses 64 bits.
    /// </summary>
    public struct VolatileLong
    {
        private long _value;

        public VolatileLong(long initialValue)
        {
            _value = initialValue;
        }

        public void Set(long newValue)
        {
            Interlocked.Exchange(ref _value, newValue);
        }

        public long Get()
        {
            return Interlocked.Read(ref _value);
        }

        public long IncrementAndGet()
        {
            Interlocked.Increment(ref _value);
            return Get();
        }

        public override string ToString()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
