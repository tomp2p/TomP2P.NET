using System;
using System.IO;

namespace TomP2P.Extensions.Netty
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

        public override ByteBuf SetIndex(int readerIndex, int writerIndex)
        {
            CheckIndex(readerIndex);
            CheckIndex(writerIndex);
            return this;
        }

        public override int Capacity
        {
            get { return 0; }
        }

        public override int MaxCapacity
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

        #region Not Implemented

        public override IByteBufAllocator Alloc
        {
            get { throw new NotImplementedException(); }
        }

        public override ByteBuf WriteByte(int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetByte(int index, int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteShort(int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetShort(int index, int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteInt(int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetInt(int index, int value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteLong(long value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetLong(int index, long value)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteBytes(sbyte[] src)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteBytes(sbyte[] src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteBytes(ByteBuf src, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteBytes(ByteBuf src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetBytes(int index, sbyte[] src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SetBytes(int index, ByteBuf src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override sbyte ReadByte()
        {
            throw new NotImplementedException();
        }

        public override byte ReadUByte()
        {
            throw new NotImplementedException();
        }

        public override sbyte GetByte(int index)
        {
            throw new NotImplementedException();
        }

        public override byte GetUByte(int index)
        {
            throw new NotImplementedException();
        }

        public override short ReadShort()
        {
            throw new NotImplementedException();
        }

        public override ushort ReadUShort()
        {
            throw new NotImplementedException();
        }

        public override short GetShort(int index)
        {
            throw new NotImplementedException();
        }

        public override ushort GetUShort(int index)
        {
            throw new NotImplementedException();
        }

        public override int ReadInt()
        {
            throw new NotImplementedException();
        }

        public override int GetInt(int index)
        {
            throw new NotImplementedException();
        }

        public override long ReadLong()
        {
            throw new NotImplementedException();
        }

        public override long GetLong(int index)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf ReadBytes(sbyte[] dst)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf ReadBytes(sbyte[] dst, int dstIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf GetBytes(int index, sbyte[] dst)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf GetBytes(int index, sbyte[] dst, int dstIndex, int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf SkipBytes(int length)
        {
            throw new NotImplementedException();
        }

        public override ByteBuf WriteZero(int length)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
