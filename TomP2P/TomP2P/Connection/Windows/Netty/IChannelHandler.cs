
using System;

namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Equivalent to Java Netty's ChannelHandler.
    /// </summary>
    public interface IChannelHandler
    {
        void ExceptionCaught(ChannelHandlerContext ctx, Exception cause);

        /// <summary>
        /// This channel is active now, which means it is open.
        /// </summary>
        /// <param name="ctx"></param>
        void ChannelActive(ChannelHandlerContext ctx);

        /// <summary>
        /// This channel is inactive now, which means it is closed.
        /// </summary>
        /// <param name="ctx"></param>
        void ChannelInactive(ChannelHandlerContext ctx);

        void HandlerAdded(ChannelHandlerContext ctx);

        void HandlerRemoved(ChannelHandlerContext ctx);
    }

    /// <summary>
    /// Interface for all inbound channel handlers.
    /// </summary>
    public interface IInboundHandler : IChannelHandler
    {
        void Read(ChannelHandlerContext ctx, object msg);

        // TODO when is this called?
        void UserEventTriggered(ChannelHandlerContext ctx, object evt);
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
