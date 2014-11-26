using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// An attempt to mimick Java's AtomicLong in .NET.
    /// </summary>
    public class AtomicLong
    {
        private long _value;

        public AtomicLong(long initialValue)
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
    }
}
