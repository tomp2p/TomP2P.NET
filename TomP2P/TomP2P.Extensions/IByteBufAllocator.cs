using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    public interface IByteBufAllocator
    {
        /// <summary>
        /// Allocate a ByteBuf with the given initial capacity and the given 
        /// maximal capacity.
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <param name="maxCapacity"></param>
        /// <returns></returns>
        ByteBuf Buffer(int initialCapacity, int maxCapacity);
    }
}
