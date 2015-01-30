
namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Equivalent to Java Netty's ChannelHandler.
    /// Does not provide any methods in .NET.
    /// </summary>
    public interface IChannelHandler
    {
        // TODO ExceptionCaught needed?
        //void ExceptionCaught(ChannelHandlerContext ctx, Exception cause);
    }

    /// <summary>
    /// Marker interface for inbound channel handlers.
    /// </summary>
    public interface IInboundHandler : IChannelHandler
    {
        void Read(ChannelHandlerContext ctx, object msg);
    }

    /// <summary>
    /// Marker interface for outbound channel handlers.
    /// </summary>
    public interface IOutboundHandler : IChannelHandler
    {
        void Write(ChannelHandlerContext ctx, object msg);
    }
}
