using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    public static class Convenient
    {
        public const long JavaLongMaxValue = 9223372036854775807; // 2^63 -1

        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Equivalent for Java's System.currentTimeMillis().
        /// </summary>
        /// <returns></returns>
        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
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
        /// Reads data from a stream until the end is reached. The
        /// data is returned as a byte array. An IOException is
        /// thrown if any of the underlying IO calls fail.
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        /// <param name="initialLength">The initial buffer length</param>
        public static byte[] ReadFully(Stream stream, int initialLength)
        {
            // If we've been passed an unhelpful initial length, just
            // use 32K.
            if (initialLength < 1)
            {
                initialLength = 32768;
            }

            byte[] buffer = new byte[initialLength];
            int read = 0;

            int chunk;
            while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
            {
                read += chunk;

                // If we've reached the end of our buffer, check to see if there's
                // any more information
                if (read == buffer.Length)
                {
                    int nextByte = stream.ReadByte();

                    // End of stream? If so, we're done
                    if (nextByte == -1)
                    {
                        return buffer;
                    }

                    // Nope. Resize the buffer, put in the byte we've just
                    // read, and continue
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[read] = (byte)nextByte;
                    buffer = newBuffer;
                    read++;
                }
            }
            // Buffer is now too big. Shrink it.
            byte[] ret = new byte[read];
            Array.Copy(buffer, ret, read);
            return ret;
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
            // TODO Java uses HeapByteBuffer, something similar in .NET
            return new MemoryStream(capacity);
        }

        /// <summary>
        /// Equivalent to Java's ByteBuffer.allocateDirect(int).
        /// Allocates a new direct byte buffer. 
        /// The new buffer's position will be zero, its limit will be its capacity, 
        /// its mark will be undefined, and each of its elements will be initialized to
        /// zero. Whether or not it has a backing array is unspecified.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static MemoryStream AllocateDirect(int p)
        {
            // TODO implement
            throw new NotImplementedException();
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
            // TODO implement
            throw new NotImplementedException();
        }
    }
}
