using System;
using System.IO;

namespace TomP2P.Message
{
    public class Buffer : IEquatable<Buffer>
    {
        public MemoryStream BackingBuffer { get; private set; }
        public int Length { get; private set; }
        public int AlreadyRead { get; private set; }

        public Buffer(MemoryStream buffer, int length)
        {
            AlreadyRead = 0;
            BackingBuffer = buffer;
            Length = length;
        }

        public Buffer(MemoryStream buffer)
        {
            AlreadyRead = 0;
            BackingBuffer = buffer;
            Length = (int)buffer.Length; // TODO check if correct equivalent of java's readablebytes (2x)
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
            // maybe use using{} body
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
            var b1 = BackingBuffer.Duplicate();
            var b2 = other.BackingBuffer.Duplicate();
            b1.Position = 0;
            b2.Position = 0;

            return b1.Equals(b2);
        }

        public override int GetHashCode()
        {
            var b = BackingBuffer.Duplicate();
            b.Position = 0;
            return b.GetHashCode() ^ Length;
        }

        public int Readable
        {
            get
            {
                var remaining = Length - AlreadyRead;
                var available = (int)BackingBuffer.Length;
                return Math.Min(remaining, available);
            }
        }

        public bool IsComplete
        {
            get { return Length == BackingBuffer.Length; }
        }

        public bool IsDone
        {
            get { return AlreadyRead == Length; }
        }
    }
}
