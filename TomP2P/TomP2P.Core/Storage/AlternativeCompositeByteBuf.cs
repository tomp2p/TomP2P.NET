using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty.Buffer;

namespace TomP2P.Core.Storage
{
    /// <summary>
    /// Equivalent of Java TomP2P's AlternativeCompositeByteBuf, which is heavily inspired 
    /// by Java Netty's AlternativeCompositeByteBuf, but with a slight different behavior.
    /// Only the needed parts are ported.
    /// </summary>
    public class AlternativeCompositeByteBuf : ByteBuf, IEquatable<ByteBuf>
    {
        private sealed class Component
        {
            public readonly ByteBuf Buf;
            public int Offset;

            internal Component(ByteBuf buf)
            {
                Buf = buf;
            }

            public int EndOffset
            {
                get { return Offset + Buf.ReadableBytes; }
            }
        }

        private static readonly IByteBufAllocator ALLOC = UnpooledByteBufAllocator.Default;

        private int _readerIndex;
        private int _writerIndex;
        private bool _freed;

        private readonly IList<Component> _components = new List<Component>();
        private readonly Component EmptyComponent = new Component(Unpooled.EmptyBuffer);
        private readonly IByteBufAllocator _alloc;
        private readonly bool _direct;

        public AlternativeCompositeByteBuf(IByteBufAllocator alloc, bool direct, params ByteBuf[] buffers)
        {
            _alloc = alloc;
            _direct = direct;
            AddComponent(buffers);
            // TODO leak needed? leak = leakDetector.open(this);
        }

        public override ByteBuf Clear()
        {
            SetReaderIndex(0);
            SetComponentWriterIndex(0);
            return this;
        }

        private Component Last()
        {
            if (_components.Count == 0)
            {
                return EmptyComponent; // TODO doesn't work correctly yet
            }
            return _components[_components.Count - 1];
        }

        public override int Capacity
        {
            get
            {
                var last = Last();
                return last.Offset + last.Buf.Capacity;
            }
        }

        public override ByteBuf SetCapacity(int newCapacity)
        {
            return SetCapacity(newCapacity, false);
        }

        // not overridden
        public AlternativeCompositeByteBuf SetCapacity(int newCapacity, bool fillBuffer)
        {
            if (newCapacity < 0 || newCapacity > MaxCapacity)
            {
                throw new ArgumentException("newCapacity: " + newCapacity);
            }

            int oldCapacity = Capacity;
            if (newCapacity > oldCapacity)
            {
                // need more storage
                int paddingLength = newCapacity - oldCapacity;
                AddComponent(fillBuffer, AllocBuffer(paddingLength));
            }
            else if (newCapacity < oldCapacity)
            {
                // remove storage
                int bytesToTrim = oldCapacity - newCapacity;
                for (var i = _components.ListIterator(_components
                        .Count); i.HasPrevious(); )
                {
                    Component c = i.Previous();
                    if (bytesToTrim >= c.Buf.Capacity)
                    {
                        bytesToTrim -= c.Buf.Capacity;
                        i.Remove();
                        // TODO c.Buf.Release(); needed?
                        continue;
                    }
                    Component newC = new Component(c.Buf.Slice(0, c.Buf.Capacity - bytesToTrim));
                    newC.Offset = c.Offset;
                    i.Set(newC);
                    break;
                }
            }

            if (ReaderIndex > newCapacity)
            {
                SetIndex(newCapacity, newCapacity);
            }
            else if (WriterIndex > newCapacity)
            {
                SetWriterIndex(newCapacity);
            }

            return this;
        }

        private ByteBuf AllocBuffer(int capacity)
        {
            if (_direct)
            {
                return Alloc.DirectBuffer(capacity);
            }
            return Alloc.HeapBuffer(capacity);
        }

        public override IByteBufAllocator Alloc
        {
            get { return _alloc; }
        }

        public override int MaxCapacity
        {
            get { return Int32.MaxValue; }
        }

        public AlternativeCompositeByteBuf AddComponent(params ByteBuf[] buffers)
        {
            return AddComponent(false, buffers);
        }

        public AlternativeCompositeByteBuf AddComponent(bool fillBuffer, params ByteBuf[] buffers)
        {
            if (buffers == null)
            {
                throw new NullReferenceException("buffers");
            }

            foreach (var b in buffers)
            {
                if (b == null)
                {
                    break;
                }
                // TODO increase reference count?
                var c = new Component(b.Duplicate()); // little-endian
                var size = _components.Count;
                _components.Add(c);
                if (size != 0)
                {
                    var prev = _components[size - 1];
                    if (fillBuffer)
                    {
                        // we plan to fill the buffer
                        c.Offset = prev.Offset + prev.Buf.Capacity;
                    }
                    else
                    {
                        // the buffer may not get filled
                        c.Offset = prev.EndOffset;
                    }
                }
                WriterIndex0(WriterIndex + c.Buf.WriterIndex);
            }
            return this;
        }

        public override int ReaderIndex
        {
            get { return _readerIndex; }
        }

        public override ByteBuf SetReaderIndex(int readerIndex)
        {
            if (readerIndex < 0 || readerIndex > _writerIndex)
            {
                throw new IndexOutOfRangeException(String.Format("readerIndex: {0} (expected: 0 <= readerIndex <= writerIndex({1}))",
                            readerIndex, _writerIndex));
            }
            _readerIndex = readerIndex;
            return this;
        }

        public override int WriterIndex
        {
            get { return _writerIndex; }
        }

        public override ByteBuf SetWriterIndex(int writerIndex)
        {
            if (writerIndex < _readerIndex || writerIndex > Capacity)
            {
                throw new IndexOutOfRangeException(String.Format("writerIndex: {0} (expected: readerIndex({1}) <= writerIndex <= capacity({2}))",
                    writerIndex, _readerIndex, Capacity));
            }
            SetComponentWriterIndex(writerIndex);
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
            SetComponentWriterIndex(writerIndex);
            return this;
        }

        private void SetComponentWriterIndex(int writerIndex)
        {
            if (WriterIndex == writerIndex)
            {
                // nothing to do
                return;
            }
            int index = FindIndex(writerIndex);
            if (index < 0)
            {
                // no component found, make sure we can write, thus adding a component
                EnsureWriteable(writerIndex);
                index = FindIndex(writerIndex);
            }
            
            int to = FindIndex(_writerIndex);
            var c = _components[index];
            int relWriterIndex = writerIndex - c.Offset;
            c.Buf.SetWriterIndex(relWriterIndex);

            if (_writerIndex < writerIndex)
            {
                // new writer index is larger than the old one
                // assuming full buffers
                for (int i = index - 1; i > to; i--)
                {
                    c = _components[i];
                    c.Buf.SetWriterIndex(c.Buf.Capacity);
                }
                _writerIndex = writerIndex;
            }
            else
            {
                // we go back in the buffer
                for (int i = index + 1; i < to; i++)
                {
                    _components[i].Buf.SetWriterIndex(0);
                }
                _writerIndex = writerIndex;
            }
        }

        public override int ReadableBytes
        {
            get { return _writerIndex - _readerIndex; }
        }

        public override bool IsReadable
        {
            get { return _writerIndex > _readerIndex; }
        }

        public override bool IsWriteable
        {
            get { return Capacity > _writerIndex; }
        }

        public override bool HasArray()
        {
            if (_components.Count == 1)
            {
                return _components[0].Buf.HasArray();
            }
            return false;
        }

        public override sbyte[] Array()
        {
            if (_components.Count == 1)
            {
                return _components[0].Buf.Array();
            }
            throw new NotSupportedException();
        }

        public override int ArrayOffset()
        {
            if (_components.Count == 1)
            {
                return _components[0].Buf.ArrayOffset();
            }
            throw new NotSupportedException();
        }

        public override ByteBuf Unwrap()
        {
            // That's indeed what TomP2P's AlternativeCompositeByteBuf does...
            return null;
        }

        public IList<ByteBuf> Decompose(int offset, int length)
        {
            CheckIndex(offset, length);
            if (length == 0)
            {
                return Convenient.EmptyList<ByteBuf>();
            }

            int componentId = FindIndex(offset);
            IList<ByteBuf> slice = new List<ByteBuf>(_components.Count);

            // the first component
            var firstC = _components[componentId];
            var first = firstC.Buf.Duplicate();
            first.SetReaderIndex(offset - firstC.Offset);

            ByteBuf buf = first;
            int bytesToSlice = length;
            do
            {
                int readableBytes = buf.ReadableBytes;
                if (bytesToSlice <= readableBytes)
                {
                    // last component
                    buf.SetWriterIndex(buf.ReaderIndex + bytesToSlice);
                    slice.Add(buf);
                    break;
                }
                else
                {
                    // not the last component
                    slice.Add(buf);
                    bytesToSlice -= readableBytes;
                    componentId++;

                    // fetch the next component
                    buf = _components[componentId].Buf.Duplicate();
                }
            } while (bytesToSlice > 0);

            // slice all component because only readable bytes are interesting
            for (int i = 0; i < slice.Count; i++)
            {
                slice[i] = slice[i].Slice();
            }

            return slice;
        }

        public override ByteBuf Slice()
        {
            return Slice(_readerIndex, ReadableBytes);
        }

        public override ByteBuf Slice(int index, int length)
        {
            if (length == 0)
            {
                return Unpooled.EmptyBuffer;
            }
            return new SlicedByteBuf(this, index, length);
        }

        public override ByteBuf Duplicate()
        {
            return new DuplicatedByteBuf(this);
        }

        public override int WriteableBytes
        {
            get { return Capacity - _writerIndex; }
        }

        public override int NioBufferCount()
        {
            if (_components.Count == 1)
            {
                return _components[0].Buf.NioBufferCount();
            }
            else
            {
                int count = 0;
                int componentsCount = _components.Count;

                for (int i = 0; i < componentsCount; i++)
                {
                    var c = _components[i];
                    count += c.Buf.NioBufferCount();
                }
                return count;
            }
        }

        public override MemoryStream NioBuffer()
        {
            return NioBuffer(ReaderIndex, ReadableBytes);
        }

        public override MemoryStream NioBuffer(int index, int length)
        {
            if (_components.Count == 1)
            {
                var buf = _components[0].Buf;
                if (buf.NioBufferCount() == 1)
                {
                    return _components[0].Buf.NioBuffer(index, length);
                }
            }
            var merged = Convenient.Allocate(length); // little-endian
            var buffers = NioBuffers(index, length);

            for (int i = 0; i < buffers.Length; i++)
            {
                merged.Put(buffers[i]);
            }

            merged.Flip();
            return merged;
        }

        public override MemoryStream[] NioBuffers()
        {
            return NioBuffers(_readerIndex, ReadableBytes);
        }

        public override MemoryStream[] NioBuffers(int index, int length)
        {
            CheckIndex(index, length);
            if (length == 0)
            {
                return new MemoryStream[0]; // EMPTY_BYTE_BUFFERS
            }

            var buffers = new List<MemoryStream>(_components.Count);
            int i = FindIndex(index);
            while (length > 0)
            {
                var c = _components[i];
                var s = c.Buf;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.ReadableBytes - (index - adjustment));

                switch (s.NioBufferCount())
                {
                    case 0:
                        throw new NotSupportedException();
                    case 1:
                        buffers.Add(s.NioBuffer(index - adjustment, localLength));
                        break;
                    default:
                        buffers.AddRange(s.NioBuffers(index - adjustment, localLength));
                        break;
                }

                index += localLength;
                length -= localLength;
                i++;
            }

            return buffers.ToArray();
        }

        #region Writes

        public override ByteBuf WriteByte(int value)
        {
            EnsureWritable0(1, true);
            SetByte(WriterIndex, value);
            IncreaseComponentWriterIndex(1);
            return this;
        }

        public override ByteBuf SetByte(int index, int value)
        {
            var c = FindComponent(index);
            c.Buf.SetByte(index - c.Offset, value);
            return this;
        }

        public override ByteBuf WriteShort(int value)
        {
            EnsureWritable0(2, true);
            SetShort(WriterIndex, value);
            IncreaseComponentWriterIndex(2);
            return this;
        }

        public override ByteBuf SetShort(int index, int value)
        {
            var c = FindComponent(index);
            if (index + 2 <= c.EndOffset)
            {
                c.Buf.SetShort(index - c.Offset, value);
            }
            // big-endian only
            else
            {
                SetByte(index, (byte)(value >> 8));
                SetByte(index + 1, (byte)value);
            }
            return this;
        }

        public override ByteBuf WriteInt(int value)
        {
            EnsureWritable0(4, true);
            SetInt(WriterIndex, value);
            IncreaseComponentWriterIndex(4);
            return this;
        }

        public override ByteBuf SetInt(int index, int value)
        {
            var c = FindComponent(index);
            if (index + 4 <= c.EndOffset)
            {
                c.Buf.SetInt(index - c.Offset, value);
            }
            // big-endian only
            else
            {
                SetShort(index, (short)(value >> 16));
                SetShort(index + 2, (short)value);
            }
            return this;
        }

        public override ByteBuf WriteLong(long value)
        {
            EnsureWritable0(8, true);
            SetLong(WriterIndex, value);
            IncreaseComponentWriterIndex(8);
            return this;
        }

        public override ByteBuf SetLong(int index, long value)
        {
            var c = FindComponent(index);
            if (index + 8 <= c.EndOffset)
            {
                c.Buf.SetLong(index - c.Offset, value);
            }
            // big-endian only
            else
            {
                SetInt(index, (int)(value >> 32));
                SetInt(index + 4, (int)value);
            }
            return this;
        }

        public override ByteBuf WriteBytes(sbyte[] src)
        {
            WriteBytes(src, 0, src.Length);
            return this;
        }

        public override ByteBuf WriteBytes(sbyte[] src, int srcIndex, int length)
        {
            EnsureWritable0(length, true);
            SetBytes(WriterIndex, src, srcIndex, length);
            IncreaseComponentWriterIndex(length);
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
                                "length({0}) exceeds src.readableBytes({1}) where src is: {2}",
                                length, src.ReadableBytes, src));
            }
            WriteBytes(src, src.ReaderIndex, length);
            src.SetReaderIndex(src.ReaderIndex + length);
            return this;
        }

        public override ByteBuf WriteBytes(ByteBuf src, int srcIndex, int length)
        {
            EnsureWritable0(length, true);
            SetBytes(WriterIndex, src, srcIndex, length);
            IncreaseComponentWriterIndex(length);
            return this;
        }

        public override ByteBuf SetBytes(int index, sbyte[] src, int srcIndex, int length)
        {
            CheckSrcIndex(index, length, srcIndex, src.Length);
            if (length == 0)
            {
                return this;
            }

            int i = FindIndex(index);
            while (length > 0)
            {
                Component c = _components[i];
                ByteBuf s = c.Buf;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.WriteableBytes);
                s.SetBytes(index - adjustment, src, srcIndex, localLength);
                index += localLength;
                srcIndex += localLength;
                length -= localLength;
                i++;
            }
            return this;
        }

        public override ByteBuf SetBytes(int index, ByteBuf src, int srcIndex, int length)
        {
            CheckSrcIndex(index, length, srcIndex, src.Capacity);
            if (length == 0)
            {
                return this;
            }

            int i = FindIndex(index);
            while (length > 0)
            {
                Component c = _components[i];
                ByteBuf s = c.Buf;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.WriteableBytes);
                s.SetBytes(index - adjustment, src, srcIndex, localLength);
                index += localLength;
                srcIndex += localLength;
                length -= localLength;
                i++;
            }
            return this;
        }

        private void IncreaseComponentWriterIndex(int increase)
        {
            int maxIncrease = 0;
            int currentIncrease = increase;
            int index = FindIndex(WriterIndex);
            while (maxIncrease < increase)
            {
                Component c = _components[index];
                int writable = c.Buf.WriteableBytes;
                writable = Math.Min(writable, currentIncrease);
                c.Buf.SetWriterIndex(c.Buf.WriterIndex + writable);
                currentIncrease -= writable;
                maxIncrease += writable;
                index++;
            }
            _writerIndex += increase;
        }

        private int CalculateNewCapacity(int minNewCapacity)
        {
            int maxCapacity = MaxCapacity;
            int threshold = 1048576 * 4; // 4 MiB page

            if (minNewCapacity == threshold)
            {
                return threshold;
            }

            // If over threshold, do not double but just increase by threshold.
            if (minNewCapacity > threshold)
            {
                int newCapacity = minNewCapacity / threshold * threshold;
                if (newCapacity > maxCapacity - threshold)
                {
                    newCapacity = maxCapacity;
                }
                else
                {
                    newCapacity += threshold;
                }
                return newCapacity;
            }

            // Not over threshold. Double up to 4 MiB, starting from 64.
            int newCapacity2 = 64;
            while (newCapacity2 < minNewCapacity)
            {
                newCapacity2 <<= 1;
            }

            return Math.Min(newCapacity2, maxCapacity);
        }

        #endregion

        #region Reads

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
            return Convert.ToByte(ReadByte()); // TODO check
        }

        public override sbyte GetByte(int index)
        {
            var c = FindComponent(index);
            return c.Buf.GetByte(index - c.Offset);
        }

        public override byte GetUByte(int index)
        {
            return Convert.ToByte(GetByte(index)); // TODO check
        }

        public override short ReadShort()
        {
            CheckReadableBytes(2);
            short v = GetShort(ReaderIndex);
            _readerIndex += 2;
            return v;
        }

        public override ushort ReadUShort()
        {
            var s = ReadShort() & 0xFFFF; // 11111111 11111111
            return (ushort) s;
        }

        public override short GetShort(int index)
        {
            var c = FindComponent(index);
            if (index + 2 <= c.EndOffset)
            {
                return c.Buf.GetShort(index - c.Offset);
            }
            // big-endian only
            else
            {
                return (short)((GetByte(index) & 0xff) << 8 | GetByte(index + 1) & 0xff); // TODO check
            }
        }

        public override ushort GetUShort(int index)
        {
            var s = GetShort(index) & 0xFFFF; // 11111111 11111111
            return (ushort) s;
        }

        public override int ReadInt()
        {
            CheckReadableBytes(4);
            int v = GetInt(ReaderIndex);
            _readerIndex += 4;
            return v;
        }

        public override int GetInt(int index)
        {
            var c = FindComponent(index);
            if (index + 4 <= c.EndOffset)
            {
                return c.Buf.GetInt(index - c.Offset);
            }
            // big-endian only
            else
            {
                return (GetShort(index) & 0xffff) << 16 | GetShort(index + 2) & 0xffff; // TODO check
            }
        }

        public override long ReadLong()
        {
            CheckReadableBytes(8);
            long v = GetLong(ReaderIndex);
            _readerIndex += 8;
            return v;
        }

        public override long GetLong(int index)
        {
            var c = FindComponent(index);
            if (index + 8 <= c.EndOffset)
            {
                return c.Buf.GetLong(index - c.Offset);
            }
            // big-endian only
            else
            {
                return (GetInt(index) & 0xffffffffL) << 32 | GetInt(index + 4) & 0xffffffffL; // TODO check
            }
        }

        public override ByteBuf ReadBytes(sbyte[] dst)
        {
            ReadBytes(dst, 0, dst.Length);
            return this;
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

        public override ByteBuf GetBytes(int index, sbyte[] dst, int dstIndex, int length)
        {
            CheckDstIndex(index, length, dstIndex, dst.Length);
            if (length == 0)
            {
                return this;
            }

            int i = FindIndex(index);
            while (length > 0)
            {
                Component c = _components[i];
                ByteBuf s = c.Buf;
                int adjustment = c.Offset;
                int localLength = Math.Min(length, s.ReadableBytes - (index - adjustment));
                s.GetBytes(index - adjustment, dst, dstIndex, localLength);
                index += localLength;
                dstIndex += localLength;
                length -= localLength;
                i++;
            }
            return this;
        }

        #endregion

        #region Stream Operations

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

            EnsureWritable0(length, true);
            CheckIndex(WriterIndex, length);

            int nLong = length >> 3;
            int nBytes = length & 7;
            for (int i = nLong; i > 0; i--)
            {
                WriteLong(0);
            }
            if (nBytes == 4)
            {
                WriteInt(0);
            }
            else if (nBytes < 4)
            {
                for (int i = nBytes; i > 0; i--)
                {
                    WriteByte((byte)0);
                }
            }
            else
            {
                WriteInt(0);
                for (int i = nBytes - 4; i > 0; i--)
                {
                    WriteByte((byte)0);
                }
            }
            return this;
        }

        #endregion

        public override ByteBuf EnsureWriteable(int minWritableBytes)
        {
            return EnsureWritable0(minWritableBytes, false);
        }

        public AlternativeCompositeByteBuf EnsureWritable0(int minWritableBytes, bool fillBuffer)
        {
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
                throw new IndexOutOfRangeException(
                    String.Format(
                        "writerIndex({0}) + minWritableBytes({1}) exceeds maxCapacity({2}): {3}",
                        WriterIndex, minWritableBytes, MaxCapacity, this));
            }

            // normalize the current capacity to the power of 2.
            int newCapacity = CalculateNewCapacity(WriterIndex + minWritableBytes);

            // Adjust to the new capacity.
            SetCapacity(newCapacity, fillBuffer);
            return this;
        }

        private int FindIndex(int offset)
        {
            CheckIndex(offset);

            var last = Last();
            if (offset >= last.Offset)
            {
                return _components.Count - 1;
            }

            int index = _components.Count - 2;
            for (var i = _components.ListIterator(_components.Count - 1); i.HasPrevious(); index--)
            {
                var c = i.Previous();
                if (offset >= c.Offset)
                {
                    return index;
                }
            }
            throw new Exception("should not happen");
        }

        private Component FindComponent(int offset)
        {
            CheckIndex(offset);

            var last = Last();
            if (offset >= last.Offset)
            {
                return last;
            }

            for (var i = _components.ListIterator(_components.Count - 1); i.HasPrevious(); )
            {
                Component c = i.Previous();
                if (offset >= c.Offset)
                {
                    return c;
                }
            }

            throw new Exception("should not happen");
        }

        private AlternativeCompositeByteBuf WriterIndex0(int writerIndex)
        {
            if (writerIndex < _readerIndex || writerIndex > Capacity)
            {
                throw new IndexOutOfRangeException(String.Format(
                    "writerIndex: {0} (expected: readerIndex({1}) <= writerIndex <= capacity({2}))",
                    writerIndex, _readerIndex, Capacity));
            }
            _writerIndex = writerIndex;
            return this;
        }

        private void CheckIndex(int index)
        {
            if (index < 0 || index > Capacity)
            {
                throw new IndexOutOfRangeException(String.Format(
                    "index: {0} (expected: range(0, {1}))", index, Capacity));
            }
        }

        private void CheckIndex(int index, int fieldLength)
        {
            if (fieldLength < 0)
            {
                throw new ArgumentException("length: " + fieldLength + " (expected: >= 0)");
            }
            if (index < 0 || index > Capacity - fieldLength)
            {
                throw new IndexOutOfRangeException(String.Format(
                    "index: {0}, length: {1} (expected: range(0, {2}))", index,
                    fieldLength, Capacity));
            }
        }

        private void CheckSrcIndex(int index, int length, int srcIndex, int srcCapacity)
        {
            CheckIndex(index, length);
            if (srcIndex < 0 || srcIndex > srcCapacity - length)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "srcIndex: {0}, length: {1} (expected: range(0, {2}))",
                        srcIndex, length, srcCapacity));
            }
        }

        private void CheckDstIndex(int index, int length, int dstIndex, int dstCapacity)
        {
            CheckIndex(index, length);
            if (dstIndex < 0 || dstIndex > dstCapacity - length)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "dstIndex: {0}, length: {1} (expected: range(0, {2}))",
                        dstIndex, length, dstCapacity));
            }
        }

        private void CheckReadableBytes(int minimumReadableBytes)
        {
            if (minimumReadableBytes < 0)
            {
                throw new ArgumentException("minimumReadableBytes: "
                        + minimumReadableBytes + " (expected: >= 0)");
            }
            if (ReaderIndex > WriterIndex - minimumReadableBytes)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "readerIndex({0}) + length({1}) exceeds writerIndex({2}): {3}",
                        ReaderIndex, minimumReadableBytes, WriterIndex, this));
            }
        }

        public static AlternativeCompositeByteBuf CompBuffer()
        {
            return CompBuffer(false);
        }

        public static AlternativeCompositeByteBuf CompDirectBuffer()
        {
            return CompBuffer(true);
        }

        public static AlternativeCompositeByteBuf CompBuffer(bool direct)
        {
            return CompBuffer(ALLOC, direct);
        }

        public static AlternativeCompositeByteBuf CompBuffer(IByteBufAllocator alloc, bool direct,
            params ByteBuf[] buffers)
        {
            return new AlternativeCompositeByteBuf(alloc, direct, buffers);
        }

        public static AlternativeCompositeByteBuf CompBuffer(params ByteBuf[] buffers)
        {
            return CompBuffer(ALLOC, false, buffers);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(GetType().Name);
            sb.Append("[ridx: ").Append(ReaderIndex)
                .Append(", widx: ").Append(WriterIndex)
                .Append(", cap: ").Append(Capacity)
                .Append(", comp: ").Append(_components.Count)
                .Append("]");
            return sb.ToString();
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
    }
}
