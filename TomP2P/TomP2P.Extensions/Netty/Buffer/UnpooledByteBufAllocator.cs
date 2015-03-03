
namespace TomP2P.Extensions.Netty.Buffer
{
    public sealed class UnpooledByteBufAllocator : AbstractByteBufAllocator
    {
        // TODO prefer direct?? needed?
        public static readonly UnpooledByteBufAllocator Default = new UnpooledByteBufAllocator(true);

        public UnpooledByteBufAllocator(bool preferDirect)
            : base(preferDirect)
        { }

        protected override ByteBuf NewDirectBuffer(int initialCapacity, int maxCapacity)
        {
            // just return an UnpooledDirectByteBuf
            ByteBuf buf = new UnpooledDirectByteBuf(this, initialCapacity, maxCapacity);
            
            // TODO toLeakAwareBuffer(buf);

            return buf;
        }

        protected override ByteBuf NewHeapBuffer(int initialCapacity, int maxCapacity)
        {
            return new UnpooledHeapByteBuf(this, initialCapacity, maxCapacity);
        }
    }
}