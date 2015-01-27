using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Netty
{
    /// <summary>
    /// Equivalent to Java Netty's ChannelHandler.
    /// Does not provide any methods in .NET.
    /// </summary>
    public interface IChannelHandler
    {
    }

    /// <summary>
    /// Marker interface for inbound channel handlers.
    /// </summary>
    public interface IInboundHandler : IChannelHandler
    {
        
    }

    /// <summary>
    /// Marker interface for outbound channel handlers.
    /// </summary>
    public interface IOutboundHandler : IChannelHandler
    {

    }
}
