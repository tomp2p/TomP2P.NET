using System;
using System.IO;

namespace TomP2P.Extensions.Netty
{
    public sealed class SlicedByteBuf : AbstractDerivedByteBuf
    {
        private readonly ByteBuf _buffer;
        private readonly int _adjustment;
        private readonly int _length;

        public SlicedByteBuf(ByteBuf buffer, int index, int length)
            : base(length)
        {
            if (index < 0 || index > buffer.Capacity - length)
            {
                throw new IndexOutOfRangeException(buffer + ".slice(" + index + ", " + length + ')');
            }

            if (buffer is SlicedByteBuf)
            {
                _buffer = ((SlicedByteBuf) buffer)._buffer;
                _adjustment = ((SlicedByteBuf)buffer)._adjustment + index;
            }
            else if (buffer is DuplicatedByteBuf)
            {
                _buffer = buffer.Unwrap();
                _adjustment = index;
            }
            else
            {
                _buffer = buffer;
                _adjustment = index;
            }
            _length = length;
            SetWriterIndex(length);
        }

        public override ByteBuf Unwrap()
        {
            return _buffer;
        }

        public override int Capacity
        {
            get { return _length; }
        }

        public override ByteBuf Duplicate()
        {
            ByteBuf duplicate = _buffer.Slice(_adjustment, _length);
            duplicate.SetIndex(ReaderIndex, WriterIndex);
            return duplicate;
        }

        public override ByteBuf Slice(int index, int length)
        {
            CheckIndex(index, length);
            if (length == 0)
            {
                return Unpooled.EmptyBuffer;
            }
            return _buffer.Slice(index + _adjustment, length);
        }

        public override int NioBufferCount()
        {
            return _buffer.NioBufferCount();
        }

        public override MemoryStream NioBuffer(int index, int length)
        {
            CheckIndex(index, length);
            return _buffer.NioBuffer(index + _adjustment, length);
        }

        public override MemoryStream[] NioBuffers(int index, int length)
        {
            CheckIndex(index, length);
            return _buffer.NioBuffers(index + _adjustment, length);
        }
    }
}
