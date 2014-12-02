using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    public class CompositeByteBuf : ByteBuf
    {
        private readonly IByteBufAllocator _alloc;
        private readonly bool _direct;
        private readonly int _maxNumComponents;

        public CompositeByteBuf(IByteBufAllocator alloc, bool direct, int maxNumComponents, params ByteBuf[] buffers)
        {
            if (alloc == null)
            {
                throw new NullReferenceException("alloc");
            }
            _alloc = alloc;
            _direct = direct;
            _maxNumComponents = maxNumComponents;
            // TODO leak detector needed?
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
            get { throw new NotImplementedException(); }
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

        public override System.IO.MemoryStream NioBuffer()
        {
            throw new NotImplementedException();
        }

        public override System.IO.MemoryStream NioBuffer(int index, int length)
        {
            throw new NotImplementedException();
        }

        public override System.IO.MemoryStream[] NioBuffers()
        {
            throw new NotImplementedException();
        }

        public override System.IO.MemoryStream[] NioBuffers(int index, int length)
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
