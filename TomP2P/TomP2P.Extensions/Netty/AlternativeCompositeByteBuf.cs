using System;
using System.Collections.Generic;
using System.IO;

namespace TomP2P.Extensions.Netty
{
    /// <summary>
    /// Equivalent of Java TomP2P's AlternativeCompositeByteBuf, which is heavily inspired 
    /// by Java Netty's AlternativeCompositeByteBuf, but with a slight different behavior.
    /// Only the needed parts are ported.
    /// </summary>
    public class AlternativeCompositeByteBuf : ByteBuf
    {
        private sealed class Component
        {
            public readonly ByteBuf Buf;
            public int Offset;

            public Component(ByteBuf buf)
            {
                Buf = buf;
            }

            public int EndOffset
            {
                get { return Offset + Buf.ReadableBytes; }
            }
        }

        private int _readerIndex;
        private int _writerIndex;
        private bool _freed;

        private readonly IList<Component> _components = new List<Component>();
        private readonly Component EmptyComponent = new Component(Unpooled.EmptyBuffer);

        public void Deallocate()
        {
            if (_freed)
            {
                return;
            }
            _freed = true;
            // TODO release needed?
            /*foreach (var c in _components)
            {
                c.Buf.Release();
            }*/
            _components.Clear();

            // TODO leak close needed?
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
                    WriterIndex0(WriterIndex + c.Buf.WriterIndex);
                }
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
            int index = FindIndex(writerIndex);
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

            var buf = first;
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
    }
}
