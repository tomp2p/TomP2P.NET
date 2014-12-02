using System;
using System.IO;

namespace TomP2P.Extensions
{
    public class EmptyByteBuf : ByteBuf
    {
        private static readonly MemoryStream EmptyByteBuffer = Convenient.AllocateDirect(0);

        public EmptyByteBuf()
        {
            // alloc not used
        }

        public override int ReadableBytes
        {
            get { return 0; }
        }

        public override int WriteableBytes
        {
            get { return 0; }
        }

        public override bool IsReadable
        {
            get { return false; }
        }

        public override bool IsWriteable
        {
            get { return false; }
        }

        public override int ReaderIndex
        {
            get { return 0; }
        }

        public override int WriterIndex
        {
            get { return 0; }
        }

        public override int Capacity
        {
            get { return 0; }
        }

        public override ByteBuf Slice()
        {
            return this;
        }

        public override ByteBuf Slice(int index, int length)
        {
            return CheckIndex(index, length);
        }

        private ByteBuf CheckIndex(int index)
        {
            if (index != 0)
            {
                throw new IndexOutOfRangeException();
            }
            return this;
        }

        private ByteBuf CheckIndex(int index, int length)
        {
            if (length < 0)
            {
                throw new ArgumentException("length: " + length);
            }
            if (index != 0 || length != 0)
            {
                throw new IndexOutOfRangeException();
            }
            return this;
        }

        public override ByteBuf Duplicate()
        {
            return this;
        }

        public override ByteBuf Unwrap()
        {
            return null;
        }

        public override MemoryStream NioBuffer()
        {
            return EmptyByteBuffer;
        }

        public override MemoryStream NioBuffer(int index, int length)
        {
            CheckIndex(index, length);
            return NioBuffer();
        }

        public override MemoryStream[] NioBuffers()
        {
            return new MemoryStream[] {EmptyByteBuffer};
        }

        public override MemoryStream[] NioBuffers(int index, int length)
        {
            CheckIndex(index, length);
            return NioBuffers();
        }

        public override int NioBufferCount()
        {
            return 1;
        }

        public override ByteBuf SetReaderIndex(int readerIndex)
        {
            return CheckIndex(readerIndex);
        }

        public override ByteBuf SetWriterIndex(int writerIndex)
        {
            return CheckIndex(writerIndex);
        }
    }
}
