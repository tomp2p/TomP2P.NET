using System.IO;

namespace TomP2P.Extensions.Netty.Buffer
{
    public abstract class AbstractDerivedByteBuf : AbstractByteBuf
    {
        protected AbstractDerivedByteBuf(int maxCapacity)
            : base(maxCapacity)
        { }

        public override MemoryStream NioBuffer(int index, int length)
        {
            return Unwrap().NioBuffer(index, length);
        }
    }
}
