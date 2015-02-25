using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Connection
{
    public class DropConnectionInboundHandler : BaseInboundHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly VolatileInteger _counter = new VolatileInteger(0);
        private readonly int _limit;

        public DropConnectionInboundHandler(int limit)
        {
            _limit = limit;
        }

        public override void ChannelActive(ChannelHandlerContext ctx)
        {
            int current;
            if ((current = _counter.IncrementAndGet()) > _limit)
            {
                ctx.Channel.Close();
                Logger.Warn("Dropped connection because {0} > {1} connections active.", current, _limit);
            }
            // fireChannelActive // TODO needed?
        }

        public override void ChannelInactive(ChannelHandlerContext ctx)
        {
            _counter.Decrement();
            // fireChannelInactive // TODO needed?
        }
    }
}
