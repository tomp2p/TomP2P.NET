using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace TomP2P.Extensions
{
    public static class Extensions
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

        public static sbyte[] ComputeHash(this string x)
        {
            HashAlgorithm algorithm = SHA1.Create();
            return (sbyte[])(Array)algorithm.ComputeHash(Encoding.UTF8.GetBytes(x)); // TODO test double cast
        }

        /// <summary>
        /// Convert a BitArray to a byte. (Only takes first 8 bits.)
        /// </summary>
        /// <param name="ba"></param>
        /// <returns></returns>
        public static sbyte ToByte(this BitArray ba)
        {
            sbyte b = 0;
            for (int i = 0; i < 8; i++)
            {
                if (ba.Get(i))
                {
                    b |= (sbyte)(1 << i); // TODO test
                }
            }
            return b;
        }

        public static bool IsIPv4(this IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetwork;
        }

        public static bool IsIPv6(this IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetworkV6;
        }

        #region Java Netty

        /// <summary>
        /// Equivalent to Java Netty's ByteBuf.writeBytes(byte[] src).
        /// </summary>
        /// <param name="s"></param>
        /// <param name="bytes"></param>
        public static void WriteBytes(this Stream s, sbyte[] bytes)
        {
            s.Write(bytes.ToByteArray(), 0, bytes.Length); // TODO test
        }

        /// <summary>
        /// Equivalent to Java Netty's ByteBuf.readableBytes().
        /// (writerPosition - readerPosition = Length - Position).
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static long ReadableBytes(this Stream s)
        {
            return s.Length - s.Position;
        }

        /// <summary>
        /// Equivalent to Java Netty's ByteBuf.duplicate().
        /// NOTE: Changes to the respective Stream instances are not mirrored as in Java Netty's ByteBuf.duplicate().
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static MemoryStream Duplicate(this Stream s)
        {
            var copy = new MemoryStream();
            s.CopyTo(copy); // TODO make async
            return copy;
        }

        /// <summary>
        /// Equivalent to Java Netty's ByteBuf.slice()
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static MemoryStream Slice(this MemoryStream ms)
        {
            byte[] data = ms.ToArray().Skip((int)ms.Position).ToArray(); // TODO test
            return new MemoryStream(data);
        }

        #endregion

        # region Conversion

        /// <summary>
        /// Converts a sbyte[] to byte[].
        /// </summary>
        /// <param name="signed">The sbyte[] to be converted.</param>
        /// <returns>The converted byte[].</returns>
        public static byte[] ToByteArray(this sbyte[] signed)
        {
            byte[] unsigned = new byte[signed.Length];
            Buffer.BlockCopy(signed, 0, unsigned, 0, signed.Length);

            return unsigned;
        }

        /// <summary>
        /// Converts a byte[] to sbyte[].
        /// </summary>
        /// <param name="unsigned">The byte[] to be converted.</param>
        /// <returns>The converted sbyte[].</returns>
        public static sbyte[] ToSByteArray(this byte[] unsigned)
        {
            // TODO test
            sbyte[] signed = new sbyte[unsigned.Length];
            Buffer.BlockCopy(unsigned, 0, signed, 0, unsigned.Length);

            return signed;
        }

        #endregion
    }
}
