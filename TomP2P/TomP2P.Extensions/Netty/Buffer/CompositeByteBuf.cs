using System;
using System.Collections.Generic;
using System.IO;

namespace TomP2P.Extensions.Netty.Buffer
{
    public class CompositeByteBuf : AbstractByteBuf
    {
        private readonly IByteBufAllocator _alloc;
        private readonly bool _direct;
        private readonly IList<Component> _components = new List<Component>();
        private readonly int _maxNumComponents;

        private sealed class Component
        {
            public readonly ByteBuf _buf;
            public readonly int _length;
            public int _offset;
            public int _endOffset;

            internal Component(ByteBuf buf)
            {
                _buf = buf;
                _length = buf.ReadableBytes;
            }
        }

        public CompositeByteBuf(IByteBufAllocator alloc, bool direct, int maxNumComponents, params ByteBuf[] buffers)
            : base(Int32.MaxValue)
        {
            if (alloc == null)
            {
                throw new NullReferenceException("alloc");
            }
            _alloc = alloc;
            _direct = direct;
            _maxNumComponents = maxNumComponents;
            // TODO leak detector needed?
        }

        public CompositeByteBuf AddComponent(ByteBuf buffer)
        {
            AddComponent0(_components.Count, buffer);
            ConsolidateIfNeeded();
            return this;
        }

        private int AddComponent0(int cIndex, ByteBuf buffer)
        {
            CheckComponentIndex(cIndex);

            if (buffer == null)
            {
                throw new NullReferenceException("buffer");
            }

            int readableBytes = buffer.ReadableBytes;
            if (readableBytes == 0)
            {
                return cIndex;
            }

            // No need to consolidate - just add a component to the list.
            var c = new Component(buffer.Slice());
            if (cIndex == _components.Count)
            {
                _components.Add(c);
                if (cIndex == 0)
                {
                    c._endOffset = readableBytes;
                }
                else
                {
                    Component prev = _components[cIndex - 1];
                    c._offset = prev._endOffset;
                    c._endOffset = c._offset + readableBytes;
                }
            }
            else
            {
                _components.Insert(cIndex, c);
                UpdateComponentOffsets(cIndex);
            }
            return cIndex;
        }

        private void UpdateComponentOffsets(int cIndex)
        {
            int size = _components.Count;
            if (size <= cIndex)
            {
                return;
            }

            var c = _components[cIndex];
            if (cIndex == 0)
            {
                c._offset = 0;
                c._endOffset = c._length;
                cIndex++;
            }

            for (int i = cIndex; i < size; i++)
            {
                var prev = _components[i - 1];
                var cur = _components[i];
                cur._offset = prev._endOffset;
                cur._endOffset = cur._offset + cur._length;
            }
        }

        private void ConsolidateIfNeeded()
        {
            // Consolidate if the number of components will exceed the allowed maximum by the current
            // operation.
            int numComponents = _components.Count;
            if (numComponents > _maxNumComponents)
            {
                int capacity = _components[numComponents - 1]._endOffset;

                ByteBuf consolidated = AllocBuffer(capacity);

                // We're not using foreach to avoid creating an iterator.
                // noinspection ForLoopReplaceableByForEach
                for (int i = 0; i < numComponents; i ++)
                {
                    Component comp = _components[i];
                    ByteBuf b = comp._buf;
                    consolidated.WriteBytes(b);
                    //comp.FreeIfNecessary();
                }
                var c = new Component(consolidated);
                c._endOffset = c._length;
                _components.Clear();
                _components.Add(c);
            }
        }

        private ByteBuf AllocBuffer(int capacity)
        {
            if (_direct)
            {
                return _alloc.DirectBuffer(capacity);
            }
            return _alloc.HeapBuffer(capacity);
        }

        private void CheckComponentIndex(int cIndex)
        {
            if (cIndex < 0 || cIndex > _components.Count)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "cIndex: {0} (expected: >= 0 && <= numComponents({1}))",
                        cIndex, _components.Count));
            }
        }

        public override IByteBufAllocator Alloc
        {
            get { throw new NotImplementedException(); }
        }

        public override int Capacity
        {
            get
            {
                if (_components.Count == 0)
                {
                    return 0;
                }
                return _components[_components.Count - 1]._endOffset;

            }
        }

        public override ByteBuf SetCapacity(int newCapacity)
        {
            throw new NotImplementedException();
        }

        public override int NioBufferCount()
        {
            if (_components.Count == 1)
            {
                return _components[0]._buf.NioBufferCount();
            }
            else
            {
                int count = 0;
                int componentsCount = _components.Count;
                for (int i = 0; i < componentsCount; i++)
                {
                    var c = _components[i];
                    count += c._buf.NioBufferCount();
                }
                return count;
            }
        }

        public override bool HasArray()
        {
            throw new NotImplementedException();
        }

        public override sbyte[] Array()
        {
            throw new NotImplementedException();
        }

        public override int ArrayOffset()
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

        public override ByteBuf GetBytes(int index, sbyte[] dst, int dstIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override MemoryStream NioBuffer(int index, int length)
        {
            if (_components.Count == 1)
            {
                ByteBuf buf = _components[0]._buf;
                if (buf.NioBufferCount() == 1)
                {
                    return _components[0]._buf.NioBuffer(index, length);
                }
            }
            MemoryStream merged = Convenient.Allocate(length); // little-endian
            MemoryStream[] buffers = NioBuffers(index, length);

            for (int i = 0; i < buffers.Length; i++)
            {
                merged.Put(buffers[i]);
            }

            merged.Flip();
            return merged;
        }

        public override MemoryStream[] NioBuffers(int index, int length)
        {
            CheckIndex(index, length);
            if (length == 0)
            {
                return new MemoryStream[0]; // EMPTY_BYTE_BUFFERS<
            }

            var buffers = new List<MemoryStream>(_components.Count);
            int i = ToComponentIndex(index);
            while (length > 0)
            {
                Component c = _components[i];
                ByteBuf s = c._buf;
                int adjustment = c._offset;
                int localLength = Math.Min(length, s.Capacity - (index - adjustment));
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

        public int ToComponentIndex(int offset)
        {
            CheckIndex(offset);

            for (int low = 0, high = _components.Count; low <= high; )
            {
                int mid = low + high >> 1;
                Component c = _components[mid];
                if (offset >= c._endOffset)
                {
                    low = mid + 1;
                }
                else if (offset < c._offset)
                {
                    high = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            throw new Exception("should not reach here");
        }

        public override MemoryStream[] NioBuffers()
        {
            return NioBuffers(ReaderIndex, ReadableBytes);
        }

        protected override void _setByte(int index, int value)
        {
            throw new NotImplementedException();
        }

        protected override void _setShort(int index, int value)
        {
            throw new NotImplementedException();
        }

        protected override void _setInt(int index, int value)
        {
            throw new NotImplementedException();
        }

        protected override void _setLong(int index, long value)
        {
            throw new NotImplementedException();
        }

        protected override sbyte _getByte(int index)
        {
            throw new NotImplementedException();
        }

        protected override short _getShort(int index)
        {
            throw new NotImplementedException();
        }

        protected override int _getInt(int index)
        {
            throw new NotImplementedException();
        }

        protected override long _getLong(int index)
        {
            throw new NotImplementedException();
        }

        // TODO implement deallocate?

        public override ByteBuf Unwrap()
        {
            return null;
        }
    }
}
