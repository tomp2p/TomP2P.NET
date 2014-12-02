using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    public class SlicedByteBuf : ByteBuf
    {
        // AbstractByteBuf
        private int _maxCapacity;

        private readonly ByteBuf _buffer;
        private readonly int _adjustment;
        private readonly int _length;

        public SlicedByteBuf(ByteBuf buffer, int index, int length)
        {
            // AbstractByteBuf
            if (length < 0)
            {
                throw new ArgumentException("maxCapacity: " + length + " (expected: >= 0)");
            }
            _maxCapacity = length;

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
            SetWriterIndex(length); // TODO fix
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

        public override System.IO.MemoryStream[] NioBuffers()
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
