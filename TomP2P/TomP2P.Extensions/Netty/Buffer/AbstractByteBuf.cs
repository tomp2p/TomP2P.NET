using System;
using System.IO;
using System.Text;

namespace TomP2P.Extensions.Netty.Buffer
{
    public abstract class AbstractByteBuf : ByteBuf, IEquatable<ByteBuf>
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

        public override ByteBuf Clear()
        {
            SetReaderIndex(0);
            SetWriterIndex(0);
            return this;
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

        public override ByteBuf WriteByte(int value)
        {
            EnsureWriteable(1);
            SetByte(_writerIndex++, value);
            return this;
        }

        public override ByteBuf SetByte(int index, int value)
        {
            CheckIndex(index);
            _setByte(index, value);
            return this;
        }

        protected abstract void _setByte(int index, int value);

        public override ByteBuf WriteShort(int value)
        {
            EnsureWriteable(2);
            _setShort(WriterIndex, value);
            _writerIndex += 2;
            return this;
        }

        public override ByteBuf SetShort(int index, int value)
        {
            CheckIndex(index, 2);
            _setShort(index, value);
            return this;
        }

        protected abstract void _setShort(int index, int value);

        public override ByteBuf WriteInt(int value)
        {
            EnsureWriteable(4);
            _setInt(WriterIndex, value);
            _writerIndex += 4;
            return this;
        }

        public override ByteBuf SetInt(int index, int value)
        {
            CheckIndex(index, 4);
            _setInt(index, value);
            return this;
        }

        protected abstract void _setInt(int index, int value);

        public override ByteBuf WriteLong(long value)
        {
            EnsureWriteable(8);
            _setLong(WriterIndex, value);
            _writerIndex += 8;
            return this;
        }

        public override ByteBuf SetLong(int index, long value)
        {
            CheckIndex(index, 8);
            _setLong(index, value);
            return this;
        }

        protected abstract void _setLong(int index, long value);

        public override ByteBuf WriteBytes(sbyte[] src)
        {
            WriteBytes(src, 0, src.Length);
            return this;
        }

        public override ByteBuf WriteBytes(sbyte[] src, int srcIndex, int length)
        {
            EnsureWriteable(length);
            SetBytes(WriterIndex, src, srcIndex, length);
            _writerIndex += length;
            return this;
        }

        public override ByteBuf WriteBytes(ByteBuf src)
        {
            WriteBytes(src, src.ReadableBytes);
            return this;
        }

        public override ByteBuf WriteBytes(ByteBuf src, int length)
        {
            if (length > src.ReadableBytes)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "length({0}) exceeds src.readableBytes({1}) where src is: {2}", length, src.ReadableBytes, src));
            }
            WriteBytes(src, src.ReaderIndex, length);
            src.SetReaderIndex(src.ReaderIndex + length);
            return this;
        }

        public override ByteBuf WriteBytes(ByteBuf src, int srcIndex, int length)
        {
            EnsureWriteable(length);
            SetBytes(WriterIndex, src, srcIndex, length);
            _writerIndex += length;
            return this;
        }

        public override sbyte ReadByte()
        {
            CheckReadableBytes(1);
            int i = ReaderIndex;
            sbyte b = GetByte(i);
            _readerIndex = i + 1;
            return b;
        }

        public override byte ReadUByte()
        {
            return Convert.ToByte(ReadByte());
        }

        public override sbyte GetByte(int index)
        {
            CheckIndex(index);
            return _getByte(index);
        }

        protected abstract sbyte _getByte(int index);

        public override byte GetUByte(int index)
        {
            return Convert.ToByte(GetByte(index));
        }

        public override short ReadShort()
        {
            CheckReadableBytes(2);
            short v = _getShort(ReaderIndex);
            _readerIndex += 2;
            return v;
        }

        public override ushort ReadUShort()
        {
            return Convert.ToUInt16(ReadShort());
        }

        public override short GetShort(int index)
        {
            CheckIndex(index, 2);
            return _getShort(index);
        }

        public override ushort GetUShort(int index)
        {
            return Convert.ToUInt16(GetShort(index));
        }

        protected abstract short _getShort(int index);

        public override int ReadInt()
        {
            CheckReadableBytes(4);
            int v = _getInt(ReaderIndex);
            _readerIndex += 4;
            return v;
        }

        public override int GetInt(int index)
        {
            CheckIndex(index, 4);
            return _getInt(index);
        }

        protected abstract int _getInt(int index);

        public override long ReadLong()
        {
            CheckReadableBytes(8);
            long v = _getLong(ReaderIndex);
            _readerIndex += 8;
            return v;
        }

        public override long GetLong(int index)
        {
            CheckIndex(index, 8);
            return _getLong(index);
        }

        protected abstract long _getLong(int index);

        public override ByteBuf ReadBytes(sbyte[] dst)
        {
            return ReadBytes(dst, 0, dst.Length);
        }

        public override ByteBuf ReadBytes(sbyte[] dst, int dstIndex, int length)
        {
            CheckReadableBytes(length);
            GetBytes(ReaderIndex, dst, dstIndex, length);
            _readerIndex += length;
            return this;
        }

        public override ByteBuf GetBytes(int index, sbyte[] dst)
        {
            return GetBytes(index, dst, 0, dst.Length);
        }

        public override ByteBuf SkipBytes(int length)
        {
            CheckReadableBytes(length);

            int newReaderIndex = ReaderIndex + length;
            if (newReaderIndex > WriterIndex)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "length: {0} (expected: readerIndex({1}) + length <= writerIndex({2}))",
                        length, ReaderIndex, WriterIndex));
            }
            _readerIndex = newReaderIndex;
            return this;
        }

        public override ByteBuf WriteZero(int length)
        {
            if (length == 0)
            {
                return this;
            }

            EnsureWriteable(length);
            CheckIndex(WriterIndex, length);

            int nLong = length >> 3;
            int nBytes = length & 7;
            for (int i = nLong; i > 0; i --) {
                WriteLong(0);
            }
            if (nBytes == 4) {
                WriteInt(0);
            } else if (nBytes < 4) {
                for (int i = nBytes; i > 0; i --) {
                    WriteByte((byte) 0);
                }
            } else {
                WriteInt(0);
                for (int i = nBytes - 4; i > 0; i --) {
                    WriteByte((byte) 0);
                }
            }
            return this;
        }

        public override ByteBuf EnsureWriteable(int minWritableBytes)
        {
            // TODO ensureAccessible();
            if (minWritableBytes < 0)
            {
                throw new ArgumentException(String.Format(
                        "minWritableBytes: {0} (expected: >= 0)", minWritableBytes));
            }

            if (minWritableBytes <= WriteableBytes)
            {
                return this;
            }

            if (minWritableBytes > MaxCapacity - WriterIndex)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "writerIndex({0}) + minWritableBytes({1}) exceeds maxCapacity({2}): {3}",
                        WriterIndex, minWritableBytes, MaxCapacity, this));
            }

            // Normalize the current capacity to the power of 2.
            int newCapacity = CalculateNewCapacity(WriterIndex + minWritableBytes);

            // Adjust to the new capacity.
            SetCapacity(newCapacity);
            return this;
        }

        private int CalculateNewCapacity(int minNewCapacity)
        {
            int maxCapacity = MaxCapacity;
            int threshold = 1048576 * 4; // 4 MiB page

            if (minNewCapacity == threshold) {
                return threshold;
            }

            // If over threshold, do not double but just increase by threshold.
            if (minNewCapacity > threshold) {
                int newCapacity = minNewCapacity / threshold * threshold;
                if (newCapacity > maxCapacity - threshold) {
                    newCapacity = maxCapacity;
                } else {
                    newCapacity += threshold;
                }
                return newCapacity;
            }

            // Not over threshold. Double up to 4 MiB, starting from 64.
            int newCapacity2 = 64;
            while (newCapacity2 < minNewCapacity) {
                newCapacity2 <<= 1;
            }

            return Math.Min(newCapacity2, maxCapacity);
        }

        protected void CheckReadableBytes(int minimumReadableBytes)
        {
            // TODO ensureAccessible();
            if (minimumReadableBytes < 0) {
                throw new ArgumentException("minimumReadableBytes: " + minimumReadableBytes + " (expected: >= 0)");
            }
            if (ReaderIndex > WriterIndex - minimumReadableBytes) {
                throw new IndexOutOfRangeException(String.Format(
                        "readerIndex({0}) + length({1}) exceeds writerIndex({2}): {3}",
                        ReaderIndex, minimumReadableBytes, WriterIndex, this));
            }
        }

        protected void CheckSrcIndex(int index, int length, int srcIndex, int srcCapacity)
        {
            CheckIndex(index, length);
            if (srcIndex < 0 || srcIndex > srcCapacity - length) {
                throw new IndexOutOfRangeException(String.Format(
                        "srcIndex: {0}, length: {1} (expected: range(0, {2}))", srcIndex, length, srcCapacity));
            }
        }

        protected void CheckDstIndex(int index, int length, int dstIndex, int dstCapacity)
        {
            CheckIndex(index, length);
            if (dstIndex < 0 || dstIndex > dstCapacity - length) {
                throw new IndexOutOfRangeException(String.Format(
                        "dstIndex: {0}, length: {1} (expected: range(0, {2}))", dstIndex, length, dstCapacity));
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as ByteBuf);
        }

        public override bool Equals(ByteBuf other)
        {
            return ByteBufUtil.Equals(this, other);
        }

        public override int GetHashCode()
        {
            return ByteBufUtil.HashCode(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(GetType().Name);
            sb.Append("[ridx: ").Append(ReaderIndex)
                .Append(", widx: ").Append(WriterIndex)
                .Append(", cap: ").Append(Capacity);
            if (MaxCapacity != Int32.MaxValue)
            {
                sb.Append("/").Append(MaxCapacity);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
