using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Connection
{
    /// <summary>
    /// Stripped-down version of the <see cref="IdleStateHandler"/>.
    /// </summary>
    public class HeartBeat : IDuplexHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const int MinTimeToHeartBeatMillis = 500;

        private readonly long _timeToHearBeatMillis;

        private VolatileLong _lastReadTime;
        private VolatileLong _lastWriteTime;



        public void Read(ChannelHandlerContext ctx, object msg)
        {
            throw new NotImplementedException();
        }

        public void Write(ChannelHandlerContext ctx, object msg)
        {
            throw new NotImplementedException();
        }
    }
}
