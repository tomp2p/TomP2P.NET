using System;

namespace TomP2P.Extensions
{
    /// <summary>
    /// A pseudo-random number generator that can be used to create the same output in Java and .NET. The underlying algorithm is inspired from Java's Random implementation.
    /// The use of this class ensures consistency, whereas Java's Random could be updated anytime.
    /// </summary>
    public class InteropRandom
    {

        private UInt64 _seed;

        public InteropRandom(ulong seed)
        {
            _seed = (seed ^ 0x5DEECE66DUL) & ((1UL << 48) - 1);
        }

        public int NextInt(int n)
        {
            if (n <= 0) throw new ArgumentException("n must be positive.");

            if ((n & -n) == n)  // i.e., n is a power of 2
                return (int)((n * (long)Next(31)) >> 31);

            long bits, val;
            do
            {
                bits = Next(31);
                val = bits % (UInt32)n;
            }
            while (bits - val + (n - 1) < 0);

            return (int)val;
        }

        protected UInt32 Next(int bits)
        {
            _seed = (_seed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);

            return (UInt32)(_seed >> (48 - bits));
        }
    }
}
