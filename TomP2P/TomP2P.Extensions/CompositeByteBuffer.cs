using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    /// <summary>
    /// Equivalent of Java TomP2P's AlternativeCompositeByteBuf, which is heavily inspired 
    /// by Java Netty's CompositeByteBuf, but with a slight different behavior.
    /// Only the needed parts are ported.
    /// </summary>
    public class CompositeByteBuffer : ByteBuf
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
        private readonly Component EmptyComponent = new Component(null); // TODO implement EmptyBuffer

        // TODO implement addComponent()
        // TODO implement decompose()
        // TODO implement slice()

        public CompositeByteBuffer AddComponent(params ByteBuf[] buffers)
        {
            return AddComponent(false, buffers);
        }

        public CompositeByteBuffer AddComponent(bool fillBuffer, params ByteBuf[] buffers)
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

        public IList<ByteBuf> Decompose(int offset, int length)
        {
            
        }

        private CompositeByteBuffer WriterIndex0(int writerIndex)
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

        public override ByteBuf SetReaderIndex(int readerIndex)
        {
            if (readerIndex < 0 || readerIndex > _writerIndex)
            {
                throw new IndexOutOfRangeException(String.Format("readerIndex: {0} (expected: 0 <= readerIndex <= writerIndex({1}))",
							readerIndex, _writerIndex));
            }
        }

        public override int ReadableBytes
        {
            get { return _writerIndex - _readerIndex; }
        }

        public override int WriteableBytes
        {
            get { return Capacity - _writerIndex; }
        }

        public override bool IsReadable
        {
            get { return _writerIndex > _readerIndex; }
        }

        public override bool IsWriteable
        {
            get { return Capacity > _writerIndex; }
        }

        public override int ReaderIndex
        {
            get { return _readerIndex; }
        }

        public override int WriterIndex
        {
            get { return _writerIndex; }
        }

        public override int Capacity
        {
            get
            {
                Component last = Last;
                return last.Offset + last.Buf.Capacity;
            }
        }

        public override ByteBuf Duplicate()
        {
            throw new NotImplementedException();
        }

        private Component Last
        {
            get
            {
                if (_components.Count == 0)
                {
                    return EmptyComponent;
                }
                return _components[_components.Count - 1];
            }
        }
    }
}
