using TomP2P.Extensions.Netty;

namespace TomP2P.Message
{
    public class CompByteBufAllocator
    {
        public AlternativeCompositeByteBuf CompDirectBuffer()
        {
            // TODO not implemented completely in .NET Netty
            return AlternativeCompositeByteBuf.CompDirectBuffer();
        }

        public AlternativeCompositeByteBuf CompBuffer()
        {
            return AlternativeCompositeByteBuf.CompBuffer();
        }
    }
}
