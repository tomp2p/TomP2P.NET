using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TomP2P.Message
{
    public class TomP2POutbound
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly bool _preferDirect;
        private readonly Encoder _encoder;
        private readonly CompByteBufAllocator _alloc;
    }
}
