using System;
using System.IO;

namespace TomP2P.Extensions.Netty
{
    public sealed class UnpooledHeapByteBuf : AbstractByteBuf
    {
        private readonly IByteBufAllocator _alloc;
        private sbyte[] _array;
        private MemoryStream _tmpNioBuf;

        public UnpooledHeapByteBuf(IByteBufAllocator alloc, int initialCapacity, int maxCapacity)
            : this(alloc, new sbyte[initialCapacity], 0, 0, maxCapacity)
        { }

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
            get { return _alloc; }
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
            // TODO< ensureAccessible();
            if (newCapacity < 0 || newCapacity > MaxCapacity)
            {
                throw new ArgumentException("newCapacity: " + newCapacity);
            }

            int oldCapacity = _array.Length;
            if (newCapacity > oldCapacity)
            {
                sbyte[] newArray = new sbyte[newCapacity];
                Array.Copy(_array, 0, newArray, 0, _array.Length);
                SetArray(newArray);
            }
            else if (newCapacity < oldCapacity)
            {
                sbyte[] newArray = new sbyte[newCapacity];
                int readerIndex = ReaderIndex;
                if (readerIndex < newCapacity)
                {
                    int writerIndex = WriterIndex;
                    if (writerIndex > newCapacity)
                    {
                        SetWriterIndex(writerIndex = newCapacity);
                    }
                    Array.Copy(_array, readerIndex, newArray, readerIndex, writerIndex - readerIndex);
                }
                else
                {
                    SetIndex(newCapacity, newCapacity);
                }
                SetArray(newArray);
            }
            return this;
        }

        public override ByteBuf GetBytes(int index, sbyte[] dst, int dstIndex, int length)
        {
            CheckDstIndex(index, length, dstIndex, dst.Length);
            Array.Copy(_array, index, dst, dstIndex, length);
            return this;
        }

        public override ByteBuf SetBytes(int index, sbyte[] src, int srcIndex, int length)
        {
            CheckSrcIndex(index, length, srcIndex, src.Length);
            Array.Copy(src, srcIndex, _array, index, length);
            return this;
        }

        public override ByteBuf SetBytes(int index, ByteBuf src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override int NioBufferCount()
        {
            return 1;
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

        public override sbyte GetByte(int index)
        {
            //ensureAccessible();
            return _getByte(index);
        }

        protected override sbyte _getByte(int index)
        {
            return _array[index];
        }

        public override short GetShort(int index)
        {
            //ensureAccessible();
            return _getShort(index);
        }

        protected override short _getShort(int index)
        {
            return (short)(_array[index] << 8 | _array[index + 1] & 0xFF); // TODO check
        }

        public override int GetInt(int index)
        {
            //ensureAccessible();
            return _getInt(index);
        }

        protected override int _getInt(int index)
        {
            // TODO check
            return (_array[index] & 0xff) << 24 |
                (_array[index + 1] & 0xff) << 16 |
                (_array[index + 2] & 0xff) << 8 |
                 _array[index + 3] & 0xff;
        }

        public override long GetLong(int index)
        {
            //ensureAccessible();
            return _getLong(index);
        }

        protected override long _getLong(int index)
        {
            // TODO check
            return ((long)_array[index] & 0xff) << 56 |
                ((long)_array[index + 1] & 0xff) << 48 |
                ((long)_array[index + 2] & 0xff) << 40 |
                ((long)_array[index + 3] & 0xff) << 32 |
                ((long)_array[index + 4] & 0xff) << 24 |
                ((long)_array[index + 5] & 0xff) << 16 |
                ((long)_array[index + 6] & 0xff) << 8 |
                 (long)_array[index + 7] & 0xff;
        }

        public override ByteBuf SetByte(int index, int value)
        {
            //ensureAccessible();
            _setByte(index, value);
            return this;
        }

        protected override void _setByte(int index, int value)
        {
            _array[index] = (sbyte)value; // TODO check
        }

        public override ByteBuf SetShort(int index, int value)
        {
            //ensureAccessible();
            _setShort(index, value);
            return this;
        }

        protected override void _setShort(int index, int value)
        {
            // TODO check
            _array[index] = (sbyte) (value >> 8);
            _array[index + 1] = (sbyte) value;
        }

        public override ByteBuf SetInt(int index, int value)
        {
            //ensureAccessible();
            _setInt(index, value);
            return this;
        }

        protected override void _setInt(int index, int value)
        {
            _array[index]     = (sbyte) (value >> 24);
            _array[index + 1] = (sbyte) (value >> 16);
            _array[index + 2] = (sbyte) (value >> 8);
            _array[index + 3] = (sbyte) value;
        }

        public override ByteBuf SetLong(int index, long value)
        {
            //ensureAccessible();
            _setLong(index, value);
            return this;
        }

        protected override void _setLong(int index, long value)
        {
            _array[index]     = (sbyte) (value >> 56);
            _array[index + 1] = (sbyte) (value >> 48);
            _array[index + 2] = (sbyte) (value >> 40);
            _array[index + 3] = (sbyte) (value >> 32);
            _array[index + 4] = (sbyte) (value >> 24);
            _array[index + 5] = (sbyte) (value >> 16);
            _array[index + 6] = (sbyte) (value >> 8);
            _array[index + 7] = (sbyte) value;
        }

        public override ByteBuf Unwrap()
        {
            return null;
        }
    }
}
