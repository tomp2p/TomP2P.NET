using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    public abstract class AbstractDerivedByteBuf : AbstractByteBuf
    {
        protected AbstractDerivedByteBuf(int maxCapacity)
            : base(maxCapacity)
        {
        }

        public override MemoryStream NioBuffer(int index, int length)
        {
            return Unwrap().NioBuffer(index, length);
        }
    }
}
