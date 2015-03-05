namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseDuplexHandler : BaseChannelHandler, IDuplexHandler
    {
        public virtual void UserEventTriggered(ChannelHandlerContext ctx, object evt)
        {
            // do nothing by default
            // can be overridden
        }

        public virtual void Read(ChannelHandlerContext ctx, object msg)
        {
            // do nothing by default
            // can be overridden
        }

        public virtual void Write(ChannelHandlerContext ctx, object msg)
        {
            // do nothing by default
            // can be overridden
        }
    }
}
