using System.IO;

namespace TomP2P.Extensions.Netty
{
    public sealed class DuplicatedByteBuf : AbstractDerivedByteBuf
    {
        private readonly ByteBuf _buffer;

        public DuplicatedByteBuf(ByteBuf buffer)
            : base(buffer.MaxCapacity)
        {
            if (buffer is DuplicatedByteBuf)
            {
                _buffer = ((DuplicatedByteBuf) buffer)._buffer;
            }
            else
            {
                _buffer = buffer;
            }
            SetIndex(buffer.ReaderIndex, buffer.WriterIndex);
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
            return _buffer.ArrayOffset();
        }

        public override ByteBuf Unwrap()
        {
            return _buffer;
        }

        public override IByteBufAllocator Alloc
        {
            get { return _buffer.Alloc; }
        }

        public override int Capacity
        {
            get { return _buffer.Capacity; }
        }

        public override ByteBuf SetCapacity(int newCapacity)
        {
            _buffer.SetCapacity(newCapacity);
            return this;
        }

        public override ByteBuf Slice(int index, int length)
        {
            return _buffer.Slice(index, length);
        }

        public override int NioBufferCount()
        {
            return _buffer.NioBufferCount();
        }

        public override MemoryStream[] NioBuffers(int index, int length)
        {
            return _buffer.NioBuffers(index, length);
        }

        public override sbyte GetByte(int index)
        {
            return _getByte(index);
        }

        protected override sbyte _getByte(int index)
        {
            return _buffer.GetByte(index);
        }

        public override short GetShort(int index)
        {
            return _getShort(index);
        }

        protected override short _getShort(int index)
        {
            return _buffer.GetShort(index);
        }

        public override int GetInt(int index)
        {
            return _getInt(index);
        }

        protected override int _getInt(int index)
        {
            return _buffer.GetInt(index);
        }

        public override long GetLong(int index)
        {
            return _getLong(index);
        }

        protected override long _getLong(int index)
        {
            return _buffer.GetLong(index);
        }

        public override ByteBuf GetBytes(int index, sbyte[] dst, int dstIndex, int length)
        {
            _buffer.GetBytes(index, dst, dstIndex, length);
            return this;
        }

        public override ByteBuf SetByte(int index, int value)
        {
            _setByte(index, value);
            return this;
        }

        protected override void _setByte(int index, int value)
        {
            _buffer.SetByte(index, value);
        }

        public override ByteBuf SetShort(int index, int value)
        {
            _setShort(index, value);
            return this;
        }

        protected override void _setShort(int index, int value)
        {
            _buffer.SetShort(index, value);
        }

        public override ByteBuf SetInt(int index, int value)
        {
            _setInt(index, value);
            return this;
        }

        protected override void _setInt(int index, int value)
        {
            _buffer.SetInt(index, value);
        }

        public override ByteBuf SetLong(int index, long value)
        {
            _setLong(index, value);
            return this;
        }

        protected override void _setLong(int index, long value)
        {
            _buffer.SetLong(index, value);
        }

        public override ByteBuf SetBytes(int index, sbyte[] src, int srcIndex, int length)
        {
            _buffer.SetBytes(index, src, srcIndex, length);
            return this;
        }

        public override ByteBuf SetBytes(int index, ByteBuf src, int srcIndex, int length)
        {
            _buffer.SetBytes(index, src, srcIndex, length);
            return this;
        }
    }
}