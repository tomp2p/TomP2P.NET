using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TomP2P.Extensions
{
    public static class Convenient
    {
        public const long JavaLongMaxValue = 9223372036854775807; // 2^63 -1

        private static readonly DateTime Jan1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Equivalent for Java's System.currentTimeMillis().
        /// </summary>
        /// <returns></returns>
        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1970).TotalMilliseconds;
        }

        /// <summary>
        /// Equivalent for Java's Array.toString(Object[]).
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static string ToString<T>(IEnumerable<T> enumerable)
        {
            return string.Join(", ", enumerable.Select(i => i.ToString()));
        }

        /// <summary>
        /// Represents a byte array in a human-readable form.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToString(this byte[] bytes)
        {
            var sb = new StringBuilder("{ ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// Equivalent for Java's Collections.emptyList().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IList<T> EmptyList<T>()
        {
            // see http://stackoverflow.com/questions/3894775/c-sharp-net-equivalent-for-java-collections-temptylist
            return new T[0];
        }

        /// <summary>
        /// Equivalent to Java's ByteBuffer.allocate(int).
        /// Allocates a new MemoryStream.
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static MemoryStream Allocate(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException();
            }
            // TODO Java uses HeapByteBuffer, something similar in .NET?
            return new MemoryStream(capacity);
        }

        /// <summary>
        /// Equivalent to Java's ByteBuffer.allocateDirect(int).
        /// Allocates a new direct byte buffer. 
        /// The new buffer's position will be zero, its limit will be its capacity, 
        /// its mark will be undefined, and each of its elements will be initialized to
        /// zero. Whether or not it has a backing array is unspecified.
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static MemoryStream AllocateDirect(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException();
            }
            // TODO Java uses DirectByteBuffer, something similar in .NET?
            return new MemoryStream(capacity);
        }

        /// <summary>
        /// Equivalent to Java's ByteBuffer.wrap(byte[], int, int).
        /// Wraps a byte array into a buffer. The new buffer will be backed by the given byte array; 
        /// that is, modifications to the buffer will cause the array to be modified and vice versa. 
        /// The new buffer's capacity will be array.length, its position will be offset, its limit 
        /// will be offset + length, and its mark will be undefined. 
        /// Its backing array will be the given array, and its array offset will be zero. 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static MemoryStream Wrap(sbyte[] array, int offset, int length)
        {
            // TODO check if correct
            var ms = new MemoryStream(array.ToByteArray(), offset, length);
            return ms;
        }
    }
}
