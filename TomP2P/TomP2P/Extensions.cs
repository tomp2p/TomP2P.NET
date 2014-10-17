using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TomP2P
{
    static class Extensions
    {
        /// <summary>
        /// Counts the leading zeros of this integer.
        /// </summary>
        /// <param name="x">The integer.</param>
        /// <returns>The amount of leading zeros in this integer.</returns>
        public static int LeadingZeros(this int x)
        {
            // taken from http://stackoverflow.com/questions/10439242/count-leading-zeroes-in-an-int32
            // see also http://aggregate.org/MAGIC/
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            return (sizeof(int) * 8 - Ones(x));
        }

        private static int Ones(int x)
        {
            x -= ((x >> 1) & 0x55555555);
            x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
            x = (((x >> 4) + x) & 0x0f0f0f0f);
            x += (x >> 8);
            x += (x >> 16);
            return (x & 0x0000003f);
        }

        public static byte[] ComputeHash(this string x)
        {
            HashAlgorithm algorithm = SHA1.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(x));
        }

        /// <summary>
        /// Copies the content of the buffer and returns a new instance (separate indexes).
        /// NOTE: Changes to the respective MemoryStream instances are not mirrored as in Java Netty's ByteBuf.duplicate().
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static MemoryStream Duplicate(this MemoryStream ms)
        {
            var copy = new MemoryStream(ms.Capacity);
            ms.CopyTo(copy); // TODO make async
            return copy;
        }
    }
}
