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

        public abstract int ReaderIndex { get; protected set; } // TODO protected?

        public abstract int WriterIndex { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="readerIndex"></param>
        /// <param name="writerIndex"></param>
        /// <returns></returns>
        public abstract ByteBuf SetIndex(int readerIndex, int writerIndex);

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
        /// Returns a slice of this buffer's sub-region.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract ByteBuf Slice(int index, int length);

        /// <summary>
        /// Returns a buffer which shares the whole region of this buffer.
        /// </summary>
        /// <returns></returns>
        public abstract ByteBuf Duplicate();

        /// <summary>
        /// Return the underlying buffer instance if this buffer is a wrapper of another buffer.
        /// </summary>
        /// <returns></returns>
        public abstract ByteBuf Unwrap();

        /// <summary>
        /// Exposes this buffer's sub-region as a MemoryStream.
        /// </summary>
        /// <returns></returns>
        public abstract MemoryStream NioBuffer();

        /// <summary>
        /// Exposes this buffer's sub-region as a MemoryStream.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract MemoryStream NioBuffer(int index, int length);

        /// <summary>
        /// Exposes this buffer's readable bytes as a MemoryStream's.
        /// </summary>
        /// <returns></returns>
        public abstract MemoryStream[] NioBuffers(); // TODO use a Java ByteBuffer wrapper

        /// <summary>
        /// Exposes this buffer's bytes as a MemoryStream's for the specified index and length.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract MemoryStream[] NioBuffers(int index, int length);

        /// <summary>
        /// Returns the maximum number of MemoryStreams that consist this buffer. Note that NioBuffers() or 
        /// NioBuffers(int, int) might return a less number of MemoryStreams.
        /// </summary>
        /// <returns></returns>
        public abstract int NioBufferCount();

        /// <summary>
        /// Sets the ReaderIndex of this buffer.
        /// </summary>
        /// <param name="readerIndex"></param>
        /// <returns></returns>
        public abstract ByteBuf SetReaderIndex(int readerIndex);

        /// <summary>
        /// Sets the WriterIndex of this buffer.
        /// </summary>
        /// <param name="writerIndex"></param>
        /// <returns></returns>
        public abstract ByteBuf SetWriterIndex(int writerIndex);
    }
}
