using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    public class UnpooledHeadByteBuf : ByteBuf
    {
        // from AbstractByteBuf
        private int _maxCapacity;
        private int _readerIndex;
        private int _writerIndex;

        private readonly IByteBufAllocator _alloc;
        private sbyte[] _array;
        private MemoryStream _tmpNioBuf;

        protected UnpooledHeadByteBuf(IByteBufAllocator alloc, sbyte[] initialArray, int maxCapacity)
            : this(alloc, initialArray, 0, initialArray.Length, maxCapacity)
        { }

        private UnpooledHeadByteBuf(IByteBufAllocator alloc, sbyte[] initialArray, int readerIndex, int writerIndex,
            int maxCapacity)
        {
            // AbstractByteBuf
            if (maxCapacity < 0)
            {
                throw new ArgumentException("maxCapacity: " + maxCapacity + " (expected: >= 0)");
            }
            _maxCapacity = maxCapacity;

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

        // from AbstractByteBuf
        private ByteBuf SetIndex(int readerIndex, int writerIndex)
        {
            if (readerIndex < 0 || readerIndex > writerIndex || writerIndex > Capacity)
            {
                throw new IndexOutOfRangeException(String.Format(
                        "readerIndex: {0}, writerIndex: {1} (expected: 0 <= readerIndex <= writerIndex <= capacity({2}))",
                        readerIndex, writerIndex, Capacity));
            }
            _readerIndex = readerIndex;
            _writerIndex = writerIndex;
            return this;
        }

        public override int ReadableBytes
        {
            get { throw new NotImplementedException(); }
        }

        public override int WriteableBytes
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsReadable
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsWriteable
        {
            get { throw new NotImplementedException(); }
        }

        public override int ReaderIndex
        {
            get { throw new NotImplementedException(); }
        }

        public override int WriterIndex
        {
            get { throw new NotImplementedException(); }
        }

        public override int Capacity
        {
            get
            {
                // TODO ensureAccessible() needed?
                return _array.Length;
            }
        }

        public override ByteBuf Slice()
        {
            throw new NotImplementedException();
        }

        public override ByteBuf Slice(int index, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf Duplicate()
        {
            throw new NotImplementedException();
        }

        public override ByteBuf Unwrap()
        {
            throw new NotImplementedException();
        }

        public override MemoryStream NioBuffer()
        {
            throw new NotImplementedException();
        }

        public override MemoryStream NioBuffer(int index, int length)
        {
            throw new NotImplementedException();
        }

        public override MemoryStream[] NioBuffers()
        {
            throw new NotImplementedException();
        }

        public override MemoryStream[] NioBuffers(int index, int length)
        {
            throw new NotImplementedException();
        }

        public override int NioBufferCount()
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetReaderIndex(int readerIndex)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetWriterIndex(int writerIndex)
        {
            throw new NotImplementedException();
        }
    }
}
