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
    }
}
