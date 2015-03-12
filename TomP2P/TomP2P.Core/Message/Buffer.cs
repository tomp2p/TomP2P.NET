using System;
using NLog;
using TomP2P.Extensions.Netty.Buffer;

namespace TomP2P.Core.Message
{
    public class Buffer : IEquatable<Buffer>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

        public Buffer AddComponent(ByteBuf slice)
        {
            if (BackingBuffer is CompositeByteBuf)
            {
                var cbb = BackingBuffer as CompositeByteBuf;
                cbb.AddComponent(slice);
                cbb.SetWriterIndex(cbb.WriterIndex + slice.ReadableBytes);
            }
            else
            {
                BackingBuffer.WriteBytes(slice);
                Logger.Debug("Buffer copied. You can use a CompositeByteBuf.");
            }
            return this;
        }

        public Object Object()
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
                var available = BackingBuffer.ReadableBytes;
                return Math.Min(remaining, available);
            }
        }

        public bool IsComplete
        {
            get { return Length == BackingBuffer.ReadableBytes; }
        }

        public bool IsDone
        {
            get { return AlreadyRead == Length; }
        }
    }
}
