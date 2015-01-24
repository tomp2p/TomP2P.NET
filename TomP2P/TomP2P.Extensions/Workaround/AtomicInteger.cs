using System.Threading;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// An attempt to mimick Java's AtomicInteger in .NET.
    /// </summary>
    public class AtomicInteger
    {
        private long _value;

        public AtomicInteger(int initialValue)
        {
            _value = initialValue;
        }

        public void Set(int newValue)
        {
            Interlocked.Exchange(ref _value, newValue);
        }

        public int Get()
        {
            return (int) Interlocked.Read(ref _value);
        }

        public int IncrementAndGet()
        {
            Interlocked.Increment(ref _value);
            return Get();
        }
    }
}
