using System;
using System.IO;

namespace TomP2P.Extensions.Netty
{
    /// <summary>
    /// Equivalent of Java Netty's ByteBuf.
    /// </summary>
    public abstract class ByteBuf
    {
        /// <summary>
        /// Returns the IByteBufAllocator which created this buffer.
        /// </summary>
        public abstract IByteBufAllocator Alloc { get; }

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
        /// Returns the maximum allowed capacity of this buffer.
        /// </summary>
        public abstract int MaxCapacity { get; }

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

        #region Writes

        public abstract ByteBuf WriteByte(int value);

        public abstract ByteBuf SetByte(int index, int value);

        public abstract ByteBuf WriteShort(int value);

        public abstract ByteBuf SetShort(int index, int value);

        public abstract ByteBuf WriteInt(int value);

        public abstract ByteBuf SetInt(int index, int value);

        public abstract ByteBuf WriteLong(long value);

        public abstract ByteBuf SetLong(int index, long value);

        public abstract ByteBuf WriteBytes(sbyte[] src);

        public abstract ByteBuf WriteBytes(sbyte[] src, int srcIndex, int length);

        public abstract ByteBuf WriteBytes(ByteBuf src, int length);

        public abstract ByteBuf WriteBytes(ByteBuf src, int srcIndex, int length);

        public abstract ByteBuf SetBytes(int index, sbyte[] src, int srcIndex, int length);

        public abstract ByteBuf SetBytes(int index, ByteBuf src, int srcIndex, int length);

        #endregion

        #region Reads

        public abstract sbyte ReadByte();

        public abstract byte ReadUByte();

        public abstract sbyte GetByte(int index);

        public abstract byte GetUByte(int index);

        public abstract short ReadShort();

        public abstract ushort ReadUShort();

        public abstract short GetShort(int index);

        public abstract ushort GetUShort(int index);

        public abstract int ReadInt();

        public abstract int GetInt(int index);

        public abstract long ReadLong();

        public abstract long GetLong(int index);

        public abstract ByteBuf ReadBytes(sbyte[] dst);

        public abstract ByteBuf ReadBytes(sbyte[] dst, int dstIndex, int length);

        public abstract ByteBuf GetBytes(int index, sbyte[] dst);

        public abstract ByteBuf GetBytes(int index, sbyte[] dst, int dstIndex, int length);

        #endregion

        #region Stream Operations

        /// <summary>
        /// Increases the current ReaderIndex by the specified length in this buffer.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract ByteBuf SkipBytes(int length);

        /// <summary>
        /// Fills this buffer with NUL (0x00) starting at the current WriterIndex and
        /// increases the WriterIndex by the specified length.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public abstract ByteBuf WriteZero(int length);

        #endregion
    }
}
