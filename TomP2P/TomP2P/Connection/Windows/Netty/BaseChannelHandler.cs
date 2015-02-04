using System;
namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseChannelHandler : IChannelHandler
    {
        public virtual void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            // do nothing by default
            // can be overridden
        }

        public virtual void ChannelActive(ChannelHandlerContext ctx)
        {
            // do nothing by default
            // can be overridden
        }

        public virtual void ChannelInactive(ChannelHandlerContext ctx)
        {
            // do nothing by default
            // can be overridden
        }
    }
}
