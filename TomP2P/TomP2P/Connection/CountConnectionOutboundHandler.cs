using System;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Connection
{
    /// <summary>
    /// This is a simple counter that counter the current open connections and total connections.
    /// </summary>
    public class CountConnectionOutboundHandler : BaseChannelHandler, IOutboundHandler, ISharable
    {
        private readonly VolatileInteger _counterCurrent = new VolatileInteger(0);
        private readonly VolatileInteger _counterTotal = new VolatileInteger(0);

        //.NET-specific: use channel activation instead of connection
        public override void ChannelActive(ChannelHandlerContext ctx)
        {
            _counterCurrent.IncrementAndGet();
            _counterTotal.IncrementAndGet();
        }

        //.NET-specific: use channel inactivation instead of close
        public override void ChannelInactive(ChannelHandlerContext ctx)
        {
            _counterCurrent.Decrement();
        }

        public void Write(ChannelHandlerContext ctx, object msg)
        {
            // nothing to do
        }

        public override IChannelHandler CreateNewInstance()
        {
            // not needed, since this handler is sharable
            throw new NotImplementedException();
        }

        public void Reset()
        {
            _counterCurrent.Set(0);
            _counterTotal.Set(0);
        }

        public int Current
        {
            get { return _counterCurrent.Get(); }
        }

        public int Total
        {
            get { return _counterTotal.Get(); }
        }
    }
}
