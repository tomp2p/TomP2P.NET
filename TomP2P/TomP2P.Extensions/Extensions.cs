using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
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

        /// <summary>
        /// Equivalent to Java's InetAddress.getBroadcast().
        /// Returns an IPAddress for the brodcast address for this IPAddress.
        /// Only IPv4 networks have a broadcast address, therefore in the case of an IPv6 network, null will be returned.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="ipv4Mask"></param>
        /// <returns>The IPAddress representing the broadcast address or null if there is no broadcast address.</returns>
        public static IPAddress GetBroadcastAddress(this IPAddress ip, IPAddress ipv4Mask)
        {
            if (ip.IsIPv4())
            {
                var complementMaskBytes = new byte[4];
                var broadcastIpBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    complementMaskBytes[i] = (byte)~ipv4Mask.GetAddressBytes()[i];
                    broadcastIpBytes[i] = (byte)(ip.GetAddressBytes()[i] | complementMaskBytes[i]);
                }
                return new IPAddress(broadcastIpBytes);
            }
            return null;
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
                //Console.WriteLine("Extension: Semaphore ({0}) waiting for {1} permits.", RuntimeHelpers.GetHashCode(s), permits);
                for (int i = 0; i < permits; i++)
                {
                    s.WaitOne();
                }
            }
        }

        /// <summary>
        /// Equivalent to Java's Semaphore.tryAcquire().
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool TryAcquire(this Semaphore s)
        {
            return s.WaitOne(TimeSpan.Zero);
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
        /// Equivalent to Java's NavigableMap.subMap(fromKey, fromInclusive, toKey, toInclusive).
        /// Returns a view of the portion of this map whose keys range from fromKey to toKey. If fromKey and 
        /// toKey are equal, the returned map is empty unless fromInclusive and toInclusive are both true.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="sd"></param>
        /// <param name="fromKey"></param>
        /// <param name="fromInclusive"></param>
        /// <param name="toKey"></param>
        /// <param name="toInclusive"></param>
        /// <returns></returns>
        public static SortedDictionary<TKey, TValue> SubDictionary<TKey, TValue>(this SortedDictionary<TKey, TValue> sd,
            TKey fromKey, bool fromInclusive, TKey toKey, bool toInclusive)
        {
            // TODO check if works
            var subDictionary = new SortedDictionary<TKey, TValue>();
            if (fromKey.Equals(toKey))
            {
                if (fromInclusive && toInclusive)
                {
                    subDictionary.Add(fromKey, sd[fromKey]);
                }
                return subDictionary;
            }
            if (fromInclusive)
            {
                subDictionary.Add(fromKey, sd[fromKey]);
            }
            if (toInclusive)
            {
                subDictionary.Add(toKey, sd[toKey]);
            }
            // find from key
            var enumerator = sd.GetEnumerator();
            do
            {
                enumerator.MoveNext();
            } while (!enumerator.Current.Equals(fromKey));
            do
            {
                subDictionary.Add(enumerator.Current.Key, enumerator.Current.Value);
            } while (!enumerator.Current.Equals(toKey));

            return subDictionary;
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
        /// <returns>The previous value associated with the key, or the default value if there
        /// was no mapping for the key.</returns>
        public static TValue Put<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue value)
        {
            var retVal = default(TValue);
            if (d.ContainsKey(key))
            {
                retVal = d[key];
                d.Remove(key);
            }
            d.Add(key, value);
            return retVal;
        }

        /// <summary>
        /// Equivalent to Java's Map.remove(key).
        /// In contrast to .NET's IDictionary.Remove(), this method returns the value that was
        /// associated with the key or the default value if there was no mapping for the key.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="d"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TValue Remove2<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
        {
            var retVal = default(TValue);
            if (d.ContainsKey(key))
            {
                retVal = d[key];
                d.Remove(key);
            }
            return retVal;
        }

        /// <summary>
        /// Equivalent to Java's List.remove(int).
        /// In contrast to .NET's IList.RemoveAt(int), this method returns the value that was
        /// associated with the index or the default value if there was none.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="l"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T RemoveAt2<T>(this IList<T> l, int index)
        {
            // TODO works?
            var retVal = l[index];
            l.RemoveAt(index);
            return retVal;
        }

        /// <summary>
        /// Equivalent to Java's Queue.peek() that returns null if empty.
        /// Retrieves, but does not remove, the head of this queue, or returns null if
        /// this queue is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="q"></param>
        /// <returns>The head of this queue, or null if this queue is empty.</returns>
        public static T Peek2<T>(this Queue<T> q) where T : class
        {
            try
            {
                return q.Count == 0 ? null : q.Peek();
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

        /// <summary>
        /// Extension to add multiple elements to a collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="c"></param>
        /// <param name="collection"></param>
        public static void AddRange<T>(this ICollection<T> c, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                c.Add(item);
            }
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

        /// <summary>
        /// Equivalent to Java Netty's Channel.isOpen().
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsOpen(this Socket s)
        {
            // see http://stackoverflow.com/questions/2661764/how-to-check-if-a-socket-is-connected-disconnected-in-c
            // TODO does this work? correctly?
            return !((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected);
        }

        #region SortedSet

        /// <summary>
        /// Equivalent to Java's NavigableSet.pollFirst().
        /// Retrieves and removes the first (lowest) element, or returns null if this set is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ss"></param>
        /// <returns></returns>
        public static T PollFirst<T>(this SortedSet<T> ss)
        {
            var first = ss.Min;
            ss.Remove(first);
            return first;
        }

        /// <summary>
        /// Equivalent to Java's SortedSet.addAll().
        /// Adds all of the elements in the specified collection to this set if they're 
        /// not already present (optional operation). If the specified collection is also 
        /// a set, the addAll operation effectively modifies this set so that its value 
        /// is the union of the two sets.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ss"></param>
        /// <param name="collection"></param>
        public static void AddAll<T>(this SortedSet<T> ss, ICollection<T> collection)
        {
            ss.UnionWith(collection);
        }

        /// <summary>
        /// Equivalent to Java's SortedSet.headSet().
        /// Returns a view of the portion of this set whose elements are strictly 
        /// less than toElement. The returned set is backed by this set, so changes 
        /// in the returned set are reflected in this set, and vice-versa. 
        /// The returned set supports all optional set operations that this set supports.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ss"></param>
        /// <param name="end"></param>
        public static SortedSet<T> HeadSet<T>(this SortedSet<T> ss, T end)
        {
            return ss.GetViewBetween(ss.Min, end);
        }

        #endregion

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
