using System.IO;

namespace TomP2P.Extensions.Netty
{
    public class DuplicatedByteBuf : AbstractDerivedByteBuf
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

        public override ByteBuf Unwrap()
        {
            return _buffer;
        }

        public override int Capacity
        {
            get { return _buffer.Capacity; }
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
    }
}