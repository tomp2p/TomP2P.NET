
using System;

namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Equivalent to Java Netty's ChannelHandler.
    /// Does not provide any methods in .NET.
    /// </summary>
    public interface IChannelHandler
    {
        void ExceptionCaught(ChannelHandlerContext ctx, Exception cause);
    }

    /// <summary>
    /// Interface for all inbound channel handlers.
    /// </summary>
    public interface IInboundHandler : IChannelHandler
    {
        void Read(ChannelHandlerContext ctx, object msg);
    }

    /// <summary>
    /// Interface for all outbound channel handlers.
    /// </summary>
    public interface IOutboundHandler : IChannelHandler
    {
        void Write(ChannelHandlerContext ctx, object msg);
    }

    /// <summary>
    /// Marker interface for all duplex (inbound and outbound) handlers.
    /// </summary>
    public interface IDuplexHandler : IInboundHandler, IOutboundHandler
    { }
}
