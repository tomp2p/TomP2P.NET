namespace TomP2P.Core.Connection.Windows.Netty
{
    public abstract class BaseInboundHandler : BaseChannelHandler, IInboundHandler
    {
        public virtual void Read(ChannelHandlerContext ctx, object msg)
        {
            // do nothing by default
            // can be overridden
        }

        public virtual void UserEventTriggered(ChannelHandlerContext ctx, object evt)
        {
            // do nothing by default
            // can be overridden
        }
    }
}
