using System;
using System.IO;
using TomP2P.Extensions.Netty;

namespace TomP2P.Message
{
    // TODO Java Buffer uses int for Length, .NET should use long

    public class Buffer : IEquatable<Buffer>
    {
        public ByteBuf BackingBuffer { get; private set; }
        public int Length { get; private set; }
        public int AlreadyRead { get; private set; }

        public Buffer(ByteBuf buffer, int length)
        {
            AlreadyRead = 0;
            BackingBuffer = buffer;
            Length = length;
        }

        public Buffer(ByteBuf buffer)
        {
            AlreadyRead = 0;
            BackingBuffer = buffer;
            Length = buffer.ReadableBytes;
        }

        public Buffer AddComponent(MemoryStream slide)
        {
            // TODO implement .NET equivalent
            throw new NotImplementedException();
        }

        public Object Object()
        {
            // TODO implement .NET equivalent
            throw new NotImplementedException();
        }

        ~Buffer()
        {
            // TODO implement .NET equivalent
            throw new NotImplementedException();
        }

        public int IncRead(int read)
        {
            AlreadyRead += read;
            return AlreadyRead;
        }

        public void Reset()
        {
            AlreadyRead = 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as Buffer);
        }

        public bool Equals(Buffer other)
        {
            if (Length != other.Length)
            {
                return false;
            }
            // TODO check correctness of porting
            var b1 = BackingBuffer.Duplicate().SetReaderIndex(0);
            var b2 = other.BackingBuffer.Duplicate().SetReaderIndex(0);

            return b1.Equals(b2);
        }

        public override int GetHashCode()
        {
            var b = BackingBuffer.Duplicate().SetReaderIndex(0);
            return b.GetHashCode() ^ Length;
        }

        public int Readable
        {
            get
            {
                var remaining = Length - AlreadyRead;
                var available = (int) BackingBuffer.ReadableBytes;
                return Math.Min(remaining, available);
            }
        }

        public bool IsComplete
        {
            get { return Length == (int) BackingBuffer.ReadableBytes; }
        }

        public bool IsDone
        {
            get { return AlreadyRead == Length; }
        }
    }
}
