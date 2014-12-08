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

        public override IByteBufAllocator Alloc
        {
            get { throw new NotImplementedException(); }
        }

        public override int Capacity
        {
            get { return _length; }
        }

        public override ByteBuf SetCapacity(int newCapacity)
        {
            throw new NotImplementedException();
        }

        public override bool HasArray()
        {
            return _buffer.HasArray();
        }

        public override sbyte[] Array()
        {
            return _buffer.Array();
        }

        public override int ArrayOffset()
        {
            return _buffer.ArrayOffset() + _adjustment;
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

        protected override void _setByte(int index, int value)
        {
            _buffer.SetByte(index + _adjustment, value);
        }

        protected override void _setShort(int index, int value)
        {
            _buffer.SetShort(index + _adjustment, value);
        }

        protected override void _setInt(int index, int value)
        {
            _buffer.SetInt(index + _adjustment, value);
        }

        protected override void _setLong(int index, long value)
        {
            _buffer.SetLong(index + _adjustment, value);
        }

        protected override sbyte _getByte(int index)
        {
            return _buffer.GetByte(index + _adjustment);
        }

        protected override short _getShort(int index)
        {
            return _buffer.GetShort(index + _adjustment);
        }

        protected override int _getInt(int index)
        {
            return _buffer.GetInt(index + _adjustment);
        }

        protected override long _getLong(int index)
        {
            return _buffer.GetLong(index + _adjustment);
        }

        public override int NioBufferCount()
        {
            return _buffer.NioBufferCount();
        }

        public override ByteBuf SetBytes(int index, sbyte[] src, int srcIndex, int length)
        {
            CheckIndex(index, length);
            _buffer.SetBytes(index + _adjustment, src, srcIndex, length);
            return this;
        }

        public override ByteBuf SetBytes(int index, ByteBuf src, int srcIndex, int length)
        {
            CheckIndex(index, length);
            _buffer.SetBytes(index + _adjustment, src, srcIndex, length);
            return this;
        }

        public override ByteBuf GetBytes(int index, sbyte[] dst, int dstIndex, int length)
        {
            CheckIndex(index, length);
            _buffer.GetBytes(index + _adjustment, dst, dstIndex, length);
            return this;
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
