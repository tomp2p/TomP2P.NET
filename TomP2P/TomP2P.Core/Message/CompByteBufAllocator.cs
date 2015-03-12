using System;
using TomP2P.Core.Storage;

namespace TomP2P.Core.Message
{
    public class CompByteBufAllocator
    {
        public AlternativeCompositeByteBuf CompDirectBuffer()
        {
            // TODO not implemented completely in .NET Netty
            throw new NotImplementedException();
            return AlternativeCompositeByteBuf.CompDirectBuffer();
        }

        public AlternativeCompositeByteBuf CompBuffer()
        {
            return AlternativeCompositeByteBuf.CompBuffer();
        }
    }
}
