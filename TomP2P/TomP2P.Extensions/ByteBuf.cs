using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    /// <summary>
    /// Equivalent of Java Netty's ByteBuf.
    /// </summary>
    public abstract class ByteBuf
    {
        /// <summary>
        /// Returns the number of readable bytes which is equal to WriterIndex - ReaderIndex.
        /// </summary>
        public abstract int ReadableBytes { get; }

        /// <summary>
        /// Returns the number of writable bytes which is equal to Capacity - WriterIndex.
        /// </summary>
        public abstract int WriteableBytes { get; }

        /// <summary>
        /// Returns true if and only if WriterIndex - ReaderIndex is greater than 0.
        /// </summary>
        public abstract bool IsReadable { get; }

        /// <summary>
        /// Returns true if and only if Capacity - WriterIndex is greater than 0.
        /// </summary>
        public abstract bool IsWriteable { get; }

        public abstract int ReaderIndex { get; }

        public abstract int WriterIndex { get; }

        /// <summary>
        /// Returns the number of bytes (octets) this buffer can contain.
        /// </summary>
        public abstract int Capacity { get; }

        /// <summary>
        /// Returns a slice of this buffer's readable bytes.
        /// </summary>
        /// <returns></returns>
        public abstract ByteBuf Slice();

        /// <summary>
        /// Returns a buffer which shares the whole region of this buffer.
        /// </summary>
        /// <returns></returns>
        public abstract ByteBuf Duplicate();

        /// <summary>
        /// Exposes this buffer's readable bytes as a MemoryStream's.
        /// </summary>
        /// <returns></returns>
        public abstract MemoryStream[] NioBuffers(); // TODO use a Java ByteBuffer wrapper

        /// <summary>
        /// Sets the ReaderIndex of this buffer.
        /// </summary>
        /// <param name="readerIndex"></param>
        /// <returns></returns>
        public abstract ByteBuf SetReaderIndex(int readerIndex);
    }
}
