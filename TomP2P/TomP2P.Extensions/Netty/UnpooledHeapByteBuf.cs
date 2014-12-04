using System;
using System.IO;

namespace TomP2P.Extensions.Netty
{
    public sealed class UnpooledHeapByteBuf : AbstractByteBuf
    {
        private readonly IByteBufAllocator _alloc;
        private sbyte[] _array;
        private MemoryStream _tmpNioBuf;

        public UnpooledHeapByteBuf(IByteBufAllocator alloc, sbyte[] initialArray, int maxCapacity)
            : this(alloc, initialArray, 0, initialArray.Length, maxCapacity)
        { }

        private UnpooledHeapByteBuf(IByteBufAllocator alloc, sbyte[] initialArray, int readerIndex, int writerIndex,
            int maxCapacity)
            : base(maxCapacity)
        {
            if (alloc == null)
            {
                throw new NullReferenceException("alloc");
            }
            if (initialArray == null)
            {
                throw new NullReferenceException("initialArray");
            }
            if (initialArray.Length > maxCapacity)
            {
                throw new ArgumentException(String.Format(
                        "initialCapacity({0}) > maxCapacity({1})", initialArray.Length, maxCapacity));
            }

            _alloc = alloc;
            SetArray(initialArray);
            SetIndex(readerIndex, writerIndex);
        }

        private void SetArray(sbyte[] initialArray)
        {
            _array = initialArray;
            _tmpNioBuf = null;
        }

        public override IByteBufAllocator Alloc
        {
            get { throw new NotImplementedException(); }
        }

        public override int Capacity
        {
            get
            {
                // TODO reference, ensureAccessible()
                return _array.Length;
            }
        }

        public override ByteBuf SetCapacity(int newCapacity)
        {
            throw new NotImplementedException();
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

        public override MemoryStream NioBuffer(int index, int length)
        {
            // TODO reference, ensureAccessible()
            return Convenient.Wrap(_array, index, length).Slice();
        }

        public override MemoryStream[] NioBuffers(int index, int length)
        {
            return new MemoryStream[] {NioBuffer(index, length)};
        }

        // TODO implement deallocate?

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
