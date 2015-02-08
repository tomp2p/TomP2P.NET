namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseOutboundHandler : BaseChannelHandler, IOutboundHandler
    {
        public virtual void Write(ChannelHandlerContext ctx, object msg)
        {
            // do nothing by default
            // can be overridden
        }
    }
}
