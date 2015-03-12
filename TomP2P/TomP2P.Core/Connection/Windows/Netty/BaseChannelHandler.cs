using System;

namespace TomP2P.Core.Connection.Windows.Netty
{
    public abstract class BaseChannelHandler : IChannelHandler
    {
        private volatile bool _isActivated;

        public virtual void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            // do nothing by default
            // can be overridden
        }

        public virtual void ChannelActive(ChannelHandlerContext ctx)
        {
            _isActivated = true;
        }

        public virtual void ChannelInactive(ChannelHandlerContext ctx)
        {
            _isActivated = false;
        }

        public virtual void HandlerAdded(ChannelHandlerContext ctx)
        {
            // do nothing by default
            // can be overridden
        }

        public virtual void HandlerRemoved(ChannelHandlerContext ctx)
        {
            // do nothing by default
            // can be overridden
        }

        public abstract IChannelHandler CreateNewInstance();

        public bool IsActivated
        {
            get { return _isActivated; }
        }
    }
}
