using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
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

        public override int Capacity
        {
            get
            {
                // TODO reference, ensureAccessible()
                return _array.Length;
            }
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

        // TODO implement deallocate?

        public override ByteBuf Unwrap()
        {
            return null;
        }
    }
}
