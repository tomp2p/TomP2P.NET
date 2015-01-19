using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TomP2P.Extensions.Workaround;

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
        /// Equivalent to Java's Semaphore.acquire(int).
        /// Acquires the given number of permits from this semaphore, blocking until all are available, or the thread is interrupted. 
        /// </summary>
        /// <param name="s">The Semaphore instance to be extended.</param>
        /// <param name="permits">The number of permits to acquire.</param>
        public static void Acquire(this Semaphore s, int permits)
        {
            // see http://stackoverflow.com/questions/20401046/why-there-is-no-javalike-semaphore-acquiring-multiple-permits-in-c
            lock (s)
            {
                for (int i = 0; i < permits; i++)
                {
                    s.WaitOne();
                }
            }
        }

        /// <summary>
        /// Equivalent to Java's Semaphore.release(int).
        /// In contrast to .NET's Semaphore.Release(int), this method allows 0 as input parameter without
        /// throwing an ArgumentOutOfRangeException.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="count">The number of times to exit the semaphore.</param>
        /// <returns>The count on the semaphore before this method was called.</returns>
        public static int Release2(this Semaphore s, int count)
        {
            return count != 0 ? s.Release(count) : 0;
        }

        /// <summary>
        /// Equivalent to Java's Map.put(key, value).
        /// In contrast to .NET's IDictionary.Add(), this method replaces the value for the key, 
        /// if another value for the same key is already stored.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="d"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Put<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue value)
        {
            if (d.ContainsKey(key))
            {
                d.Remove(key);
            }
            d.Add(key, value);
        }

        /// <summary>
        /// Equivalent to Java's Queue.peek() that returns null if empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="q"></param>
        /// <returns></returns>
        public static T Peek2<T>(this Queue<T> q) where T : class
        {
            try
            {
                return q.Peek();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Equivalent to Java's List.listIterator(int).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static ListIterator<T> ListIterator<T>(this IList<T> list, int index) where T : class
        {
            return new ListIterator<T>(list, index);
        }

        public static bool ScrambledEquals<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
        {
            // TODO check if works
            // from http://stackoverflow.com/questions/3669970/compare-two-listt-objects-for-equality-ignoring-order
            var cnt = new Dictionary<T, int>();
            foreach (T s in list1)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]++;
                }
                else
                {
                    cnt.Add(s, 1);
                }
            }
            foreach (T s in list2)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]--;
                }
                else
                {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
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

        /// <summary>
        /// Equivalent to Java's BitSet.toByteArray().
        /// </summary>
        /// <param name="ba"></param>
        /// <returns></returns>
        public static sbyte[] ToByteArray(this BitArray ba)
        {
            // TODO check if same result as with Java
            var bytes = new sbyte[(ba.Length - 1) / 8 + 1];
            ba.CopyTo(bytes, 0);
            return bytes;
        }

        /// <summary>
        /// Equivalent to Java's BitSet.flip(int, int).
        /// Sets each bit from the specified fromIndex (inclusive) to the specified toIndex (exclusive) to the complement of its current value.
        /// </summary>
        /// <param name="ba"></param>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        public static void Flip(this BitArray ba, int fromIndex, int toIndex)
        {
            for (int i = fromIndex; i < toIndex; i++) // TODO check if works correctly
            {
                ba[i] = !ba[i];
            }
        }

        /// <summary>
        /// Equivalent to Java's BitSet.equals() implementation.
        /// </summary>
        /// <param name="ba"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool Equals2(this BitArray ba, BitArray other)
        {
            if (ba.Length != other.Length)
            {
                return false;
            }
            for (int i = 0; i < ba.Length; i++)
            {
                if (ba[i] != other[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsIPv4(this IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetwork;
        }

        public static bool IsIPv6(this IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetworkV6;
        }

        #region Java ByteBuffer

        /// <summary>
        /// Equivalent of Java's ByteBuffer.put(ByteBuffer).
        /// This method transfers the bytes remaining in the given source buffer into this buffer. 
        /// If there are more bytes remaining in the source buffer than in this buffer, that is, 
        /// if src.remaining() > remaining(), then no bytes are transferred and an overflow exception
        /// is thrown.
        /// Otherwise, this method copies n = src.remaining() bytes from the given buffer into this 
        /// buffer, starting at each buffer's current position. The positions of both buffers are 
        /// then incremented by n. 
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        public static MemoryStream Put(this MemoryStream ms, MemoryStream src)
        {
            // TODO check if works
            /*if (src.Remaining() > ms.Remaining())
            {
                throw new OverflowException("src.remaining() > ms.remaining()");
            }*/
            var bytes = new byte[src.Remaining()];
            Array.Copy(src.ToArray(), src.Position, bytes, 0, src.Remaining());

            ms.Write(bytes, 0, bytes.Length);
            return ms;
        }

        /// <summary>
        /// Equivalent of Java's Buffer.flip().
        /// Flips this buffer. The limit is set to the current position and then the 
        /// position is set to zero.
        /// </summary>
        /// <param name="ms"></param>
        public static void Flip(this MemoryStream ms)
        {
            // TODO check if works
            ms.SetLength(ms.Position);
            ms.Position = 0;
        }

        /// <summary>
        /// Equivalent of Java's ByteBuffer.slice().
        /// Attention: bytes are copied!
        /// Creates a new byte buffer whose content is a shared subsequence of this buffer's content.
        /// The content of the new buffer will start at this buffer's current position.
        /// The new buffer's position will be zero, its capacity and its limit will be the number of
        /// bytes remaining in this buffer, and its mark will be undefined.
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static MemoryStream Slice(this MemoryStream ms)
        {
            // in contrast to Java's version, the bytes are copied, not referenced
            // check http://stackoverflow.com/questions/1646193/why-does-memorystream-getbuffer-always-throw
            var bytes = new byte[ms.Remaining()];
            Array.Copy(ms.ToArray(), 0, bytes, 0, bytes.Length);
            var slice = new MemoryStream(bytes);
            return slice;
        }

        /// <summary>
        /// Equivalent of Java's ByteBuffer.remaining().
        /// Returns the number of elements between the current position and the limit.
        /// </summary>
        /// <param name="ms">The MemoryStream instance to be extended.</param>
        /// <returns>The number of bytes remaining in this buffer.</returns>
        public static long Remaining(this MemoryStream ms)
        {
            return ms.Length - ms.Position;
        }

        /// <summary>
        /// Equivalent of Java's ByteBuffer.get(byte[], int, int).
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="dst"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public static void Get(this MemoryStream ms, byte[] dst, long offset, long length)
        {
            Array.Copy(ms.ToArray(), offset, dst, 0, length);
        }

        #endregion

        /*#region Java Netty

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
            // TODO correct when implementing a .NET ByteBuf
            return s.Remaining();
        }*/

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
        /// Equivalent to Java's Buffer.limit(int).
        /// Sets this buffer's limit.  If the position is larger than the new limit 
        /// then it is set to the new limit.
        /// </summary>
        /// <param name="mjs"></param>
        /// <param name="newLimit"></param>
        /// <returns></returns>
        public static MemoryStream Limit(this MemoryStream ms, int newLimit)
        {
            // TODO check if correct
            if (newLimit > ms.Capacity || newLimit < 0)
            {
                throw new ArgumentException();
            }
            ms.SetLength(newLimit);
            if (ms.Position > ms.Length)
            {
                ms.Position = ms.Length;
            }
            return ms;
        }

        /*/// <summary>
        /// Equivalent to Java Netty's ByteBuf.slice().
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static MemoryStream Slice(this MemoryStream ms)
        {
            byte[] data = ms.ToArray().Skip((int)ms.Position).ToArray(); // TODO test
            return new MemoryStream(data);
        }

        /// <summary>
        /// Equivalent to Java Netty's ByteBuf.readerIndex(int).
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="index"></param>
        public static void SetReaderIndex(this JavaBinaryReader reader, long index)
        {
            reader.BaseStream.Position = index;
        }

        public static long WriterIndex(this JavaBinaryWriter writer)
        {
            return writer.BaseStream.Position;
        }

        public static long ReaderIndex(this JavaBinaryReader reader)
        {
            return reader.BaseStream.Position;
        }

        public static void SkipBytes(this Stream s, int length)
        {
            for (int i = 0; i < length; i++)
            {
                s.ReadByte();
            }
        }

        public static long ReadableBytes(this JavaBinaryReader reader)
        {
            return reader.BaseStream.ReadableBytes();
        }

        /// <summary>
        /// Equivalent to Java Netty's ByteBuf.isReadable().
        /// Returns true if and only if (this.writerIndex - this.readerIndex) is greater than 0.
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public static bool IsReadable(this JavaBinaryWriter writer)
        {
            // TODO implement
            return false;
        }

        #endregion*/

        #region Conversion

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
            sbyte[] signed = new sbyte[unsigned.Length];
            Buffer.BlockCopy(unsigned, 0, signed, 0, unsigned.Length);

            return signed;
        }

        #endregion
    }
}
