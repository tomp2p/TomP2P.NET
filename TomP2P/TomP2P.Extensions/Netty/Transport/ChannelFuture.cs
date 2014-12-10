using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Netty.Transport
{
    public interface IChannelFuture // TODO correct Future<V> equivalent?
    {
        IChannel Channel();

        bool Cancel(bool mayInterruptIfRunning); // in Java, implemented in Future<V>
    }
}
