using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Peers
{
    /// <summary>
    /// This class stores the location and domain key.
    /// </summary>
    public sealed class Number320
    {
        public static readonly Number320 Zero = new Number320(Number160.Zero, Number160.Zero);

        public Number320(Number160 locationKey, Number160 domainKey)
        {
            throw new Exception();
        }
    }
}
