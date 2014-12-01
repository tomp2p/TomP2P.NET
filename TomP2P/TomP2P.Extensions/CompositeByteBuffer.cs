using System;
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
        private int _readerIndex;
        private int _writerIndex;

        private readonly IList<Component> _components = new List<Component>();

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
                // TODO increade reference count?
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









        private sealed class Component
        {
            public readonly ByteBuf Buf;
            public long Offset;

            public Component(ByteBuf buf)
            {
                Buf = buf;
            }

            public long EndOffset
            {
                get { return Offset + Buf.ReadableBytes; }
            }
        }

        public override int ReadableBytes
        {
            get { throw new NotImplementedException(); }
        }

        public override int WriterIndex
        {
            get { return _writerIndex; }
        }

        public override ByteBuf Duplicate()
        {
            throw new NotImplementedException();
        }
    }
}
