using System;
using System.IO;

namespace TomP2P.Extensions.Netty
{
    public abstract class AbstractByteBuf : ByteBuf
    {
        private int _readerIndex;
        private int _writerIndex;
        private readonly int _maxCapacity;

        protected AbstractByteBuf(int maxCapacity)
        {
            if (maxCapacity < 0)
            {
                throw new ArgumentException("maxCapacity: " + maxCapacity + " (expected: >= 0)");
            }
            _maxCapacity = maxCapacity;
        }

        public override int ReaderIndex
        {
            get { return _readerIndex; }
        }

        public override int WriterIndex
        {
            get { return _writerIndex; }
        }

        public override int MaxCapacity
        {
            get { return _maxCapacity; }
        }

        public override ByteBuf SetReaderIndex(int readerIndex)
        {
            if (readerIndex < 0 || readerIndex > WriterIndex)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "readerIndex: {0} (expected: 0 <= readerIndex <= writerIndex({1}))", readerIndex, WriterIndex));
            }
            _readerIndex = readerIndex;
            return this;
        }

        public override ByteBuf SetWriterIndex(int writerIndex)
        {
            if (writerIndex < ReaderIndex || writerIndex > Capacity)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "writerIndex: {0} (expected: readerIndex({1}) <= writerIndex <= capacity({2}))",
                        writerIndex, ReaderIndex, Capacity));
            }
            _writerIndex = writerIndex;
            return this;
        }

        public override ByteBuf SetIndex(int readerIndex, int writerIndex)
        {
            if (readerIndex < 0 || readerIndex > writerIndex || writerIndex > Capacity)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "readerIndex: {0}, writerIndex: {1} (expected: 0 <= readerIndex <= writerIndex <= capacity({2}))",
                        readerIndex, writerIndex, Capacity));
            }
            _readerIndex = readerIndex;
            _writerIndex = writerIndex;
            return this;
        }

        public override bool IsReadable
        {
            get { return WriterIndex > ReaderIndex; }
        }

        public override bool IsWriteable
        {
            get { return Capacity > WriterIndex; }
        }

        public override int ReadableBytes
        {
            get { return WriterIndex - ReaderIndex; }
        }

        public override int WriteableBytes
        {
            get { return Capacity - WriterIndex; }
        }

        // TODO maybe implement read/writes here

        public override ByteBuf Duplicate()
        {
            return new DuplicatedByteBuf(this);
        }

        public override ByteBuf Slice()
        {
            return Slice(ReaderIndex, ReadableBytes);
        }

        public override ByteBuf Slice(int index, int length)
        {
            if (length == 0)
            {
                return Unpooled.EmptyBuffer;
            }
            return new SlicedByteBuf(this, index, length);
        }

        public override MemoryStream NioBuffer()
        {
            return NioBuffer(ReaderIndex, ReadableBytes);
        }

        public override MemoryStream[] NioBuffers()
        {
            return NioBuffers(ReaderIndex, ReadableBytes);
        }

        // TODO implement comparable, equals, hashcode?

        protected void CheckIndex(int index)
        {
            // TODO reference, ensureAccessible();
            if (index < 0 || index >= Capacity) {
                throw new IndexOutOfRangeException(String.Format(
                        "index: {0} (expected: range(0, {1}))", index, Capacity));
            }
        }

        protected void CheckIndex(int index, int fieldLength)
        {
            // TODO reference, ensureAccessible();
            if (fieldLength < 0) {
                throw new ArgumentException("length: " + fieldLength + " (expected: >= 0)");
            }
            if (index < 0 || index > Capacity - fieldLength) {
                throw new IndexOutOfRangeException(String.Format(
                        "index: {0}, length: {1} (expected: range(0, {2}))", index, fieldLength, Capacity));
            }
        }

        #region Not Implemented

        public override IByteBufAllocator Alloc
        {
            get { throw new NotImplementedException(); }
        }

        public override ByteBuf WriteByte(int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetByte(int index, int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteShort(int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetShort(int index, int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteInt(int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetInt(int index, int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteLong(long value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetLong(int index, long value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteBytes(sbyte[] src)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteBytes(sbyte[] src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteBytes(ByteBuf src, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteBytes(ByteBuf src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetBytes(int index, sbyte[] src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetBytes(int index, ByteBuf src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override sbyte ReadByte()
        {
            throw new NotImplementedException();
        }

        public override byte ReadUByte()
        {
            throw new NotImplementedException();
        }

        public override sbyte GetByte(int index)
        {
            throw new NotImplementedException();
        }

        public override byte GetUByte(int index)
        {
            throw new NotImplementedException();
        }

        public override short ReadShort()
        {
            throw new NotImplementedException();
        }

        public override ushort ReadUShort()
        {
            throw new NotImplementedException();
        }

        public override short GetShort(int index)
        {
            throw new NotImplementedException();
        }

        public override ushort GetUShort(int index)
        {
            throw new NotImplementedException();
        }

        public override int ReadInt()
        {
            throw new NotImplementedException();
        }

        public override int GetInt(int index)
        {
            throw new NotImplementedException();
        }

        public override long ReadLong()
        {
            throw new NotImplementedException();
        }

        public override long GetLong(int index)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf ReadBytes(sbyte[] dst)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf ReadBytes(sbyte[] dst, int dstIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf GetBytes(int index, sbyte[] dst)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf GetBytes(int index, sbyte[] dst, int dstIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SkipBytes(int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteZero(int length)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
