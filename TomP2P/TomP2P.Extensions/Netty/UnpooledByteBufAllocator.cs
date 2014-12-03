using System;

namespace TomP2P.Extensions.Netty
{
    public sealed class UnpooledByteBufAllocator : IByteBufAllocator
    {
        // TODO preferdirect?? needed?
        public static readonly UnpooledByteBufAllocator Default = new UnpooledByteBufAllocator(true);

        private readonly bool _directByDefault;
        private readonly ByteBuf _emptyBuf;

        public UnpooledByteBufAllocator(bool preferDirect)
        {
            // from AbstractByteBufAllocator
            _directByDefault = preferDirect; // TODO PlatformDependent needed??
            _emptyBuf = new EmptyByteBuf(); // alloc not used
        }

        public ByteBuf Buffer(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity != 0 || maxCapacity != 0)
            {
                throw new ArgumentException("Port does not support calls other than (0, 0).");
            }
            return new EmptyByteBuf();
        }

        // from AbstractByteBufAllocator
        public ByteBuf DirectBuffer(int initialCapacity)
        {
            return DirectBuffer(initialCapacity, Int32.MaxValue);
        }

        // from AbstractByteBufAllocator
        public ByteBuf DirectBuffer(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity == 0 && maxCapacity == 0)
            {
                return new EmptyByteBuf();
            }
            Validate(initialCapacity, maxCapacity);
            return NewDirectBuffer(initialCapacity, maxCapacity);
        }

        private ByteBuf NewDirectBuffer(int initialCapacity, int maxCapacity)
        {
            ByteBuf buf;
            // just return an UnpooledDirectByteBuf
            buf = new UnpooledDirectByteBuf(this, initialCapacity, maxCapacity);

            // TODO toLeadAwareBuffer() needed?
            return buf;
        }

        // from AbstractByteBufAllocator
        private static void Validate(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentException("initialCapacity: " + initialCapacity + " (expectd: 0+)");
            }
            if (initialCapacity > maxCapacity)
            {
                throw new ArgumentException(String.Format(
                        "initialCapacity: {0} (expected: not greater than maxCapacity({1})",
                        initialCapacity, maxCapacity));
            }
        }
    }
}