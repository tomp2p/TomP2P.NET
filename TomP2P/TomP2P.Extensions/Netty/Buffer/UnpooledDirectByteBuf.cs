using System;
using System.IO;

namespace TomP2P.Extensions.Netty.Buffer
{
    public sealed class UnpooledDirectByteBuf : AbstractByteBuf
    {
        private readonly IByteBufAllocator _alloc;

        private MemoryStream _buffer;
        //private MemoryStream _tmpNioBuf;
        private int _capacity;
        private bool _doNotFree;

        public UnpooledDirectByteBuf(IByteBufAllocator alloc, int initialCapacity, int maxCapacity)
            : base(maxCapacity)
        {
            if (alloc == null)
            {
                throw new NullReferenceException("alloc");
            }
            if (initialCapacity < 0)
            {
                throw new ArgumentException("initialCapacity: " + initialCapacity);
            }
            if (maxCapacity < 0)
            {
                throw new ArgumentException("maxCapacity: " + maxCapacity);
            }
            if (initialCapacity > maxCapacity)
            {
                throw new ArgumentException(String.Format(
                        "initialCapacity({0}) > maxCapacity({1})", initialCapacity, maxCapacity));
            }

            _alloc = alloc;
            SetByteBuffer(Convenient.AllocateDirect(initialCapacity));
        }

        private void SetByteBuffer(MemoryStream buffer)
        {
            MemoryStream oldBuffer = _buffer;
            if (oldBuffer != null)
            {
                if (_doNotFree)
                {
                    _doNotFree = false;
                }
                else
                {
                    FreeDirect(oldBuffer);
                }
            }

            _buffer = buffer;
            //_tmpNioBuf = null;
            _capacity = (int)buffer.Remaining(); // TODO unsafe cast
        }

        private void FreeDirect(MemoryStream buffer)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public override int Capacity
        {
            get { return _capacity; }
        }

        public override ByteBuf SetCapacity(int newCapacity)
        {
            throw new NotImplementedException();
        }

        public override IByteBufAllocator Alloc
        {
            get { return _alloc; }
        }

        public override bool HasArray()
        {
            return false;
        }

        public override sbyte[] Array()
        {
            throw new NotSupportedException("direct buffer");
        }

        public override int ArrayOffset()
        {
            throw new NotSupportedException("direct buffer");
        }

        public override int NioBufferCount()
        {
            return 1;
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

        public override MemoryStream[] NioBuffers(int index, int length)
        {
            return new MemoryStream[] {NioBuffer(index, length)};
        }

        public override MemoryStream NioBuffer(int index, int length)
        {
            CheckIndex(index, length);
            var copy = _buffer.Duplicate();
            copy.Position = index;
            copy.Limit(index + length).Slice();
            return copy;
        }

        public override ByteBuf Unwrap()
        {
            return null;
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
    }
}
