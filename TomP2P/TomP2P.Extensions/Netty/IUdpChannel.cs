using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Netty
{
    public interface IUdpChannel : IChannel
    {
        bool IsOpen { get; }
    }
}
