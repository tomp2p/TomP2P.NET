using System.Globalization;
using System.Threading;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// An attempt to mimick Java's AtomicInteger in .NET.
    /// In .NET, however, it is reasonable to make it a struct rather than a class.
    /// </summary>
    public struct VolatileInteger
    {
        private int _value;

        public VolatileInteger(int initialValue)
        {
            _value = initialValue;
        }

        public void Set(int newValue)
        {
            Interlocked.Exchange(ref _value, newValue);
        }

        public int Get()
        {
            var longVal = (long) _value;
            return (int) Interlocked.Read(ref longVal);
        }

        public int IncrementAndGet()
        {
            Interlocked.Increment(ref _value);
            return Get();
        }

        public int DecrementAndGet()
        {
            Interlocked.Decrement(ref _value);
            return Get();
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref _value);
        }

        public override string ToString()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
