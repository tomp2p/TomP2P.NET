using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    public sealed class UnpooledByteBufAllocator : IByteBufAllocator
    {
        // TODO preferdirect?? needed?
        public static readonly UnpooledByteBufAllocator Default = new UnpooledByteBufAllocator(false);

        public ByteBuf Buffer(int initialCapacity, int maxCapacity)
        {
            throw new NotImplementedException();
        }

        private readonly bool _directByDefault;
        private readonly ByteBuf _emptyBuf;

        public UnpooledByteBufAllocator(bool preferDirect)
        {
            // from AbstractByteBufAllocator
            _directByDefault = preferDirect; // TODO PlatformDependent needed??
            _emptyBuf = new EmptyByteBuf(this);
        }
    }
}
