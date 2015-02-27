using System;
using System.Threading;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Connection
{
    /// <summary>
    /// Stripped-down version of the TimeoutFactory.IdleStateHandler.
    /// </summary>
    public class HeartBeat : BaseDuplexHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const int MinTimeToHeartBeatMillis = 500;

        public long TimeToHeartBeatMillis { get; private set; }

        private readonly VolatileLong _lastReadTime = new VolatileLong(0);
        private readonly VolatileLong _lastWriteTime = new VolatileLong(0);

        // .NET-specific
        private ExecutorService _executor;
        private volatile Timer _timer;

        private volatile int _state; // 0 - none, 1 - initialized, 2- destroyed

        private readonly IPingBuilderFactory _pingBuilderFactory;

        // may be set from other threads
        private volatile PeerConnection _peerConnection;

        public HeartBeat(long allIdleTimeMillis, IPingBuilderFactory pingBuilderFactory)
        {
            if (allIdleTimeMillis <= 0)
            {
                TimeToHeartBeatMillis = 0;
            }
            else
            {
                // TODO check correctness, same in Java?
                TimeToHeartBeatMillis = Math.Max(allIdleTimeMillis, MinTimeToHeartBeatMillis);
            }
            _pingBuilderFactory = pingBuilderFactory;
        }


        public override void Read(ChannelHandlerContext ctx, object msg)
        {
            _lastReadTime.Set(Convenient.CurrentTimeMillis());
            ctx.FireRead(msg);
        }

        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            ctx.Channel.WriteCompleted += channel => _lastWriteTime.Set(Convenient.CurrentTimeMillis());
            //ctx.FireWrite(msg); // TODO needed?
        }

        public PeerConnection PeerConnection
        {
            get { return _peerConnection; }
        }

        public HeartBeat SetPeerConnection(PeerConnection peerConnection)
        {
            _peerConnection = peerConnection;
            return this;
        }

        public override void HandlerAdded(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.IsActive)
            {
                Initialize(ctx);
            }
        }

        public override void ChannelActive(ChannelHandlerContext ctx)
        {
            // invoked when a pipeline is attached to a channel
            Initialize(ctx);
        }

        public override void HandlerRemoved(ChannelHandlerContext ctx)
        {
            Destroy();
        }

        public override void ChannelInactive(ChannelHandlerContext ctx)
        {
            // invoked when a socket/channel is closed
            Destroy();
        }

        private void Initialize(ChannelHandlerContext ctx)
        {
            switch (_state)
            {
                case 1:
                    return;
                case 2:
                    return;
            }
            _state = 1;

            // .NET-specific:
            if (_executor == null)
            {
                _executor = new ExecutorService();
            }
            var currentMillis = Convenient.CurrentTimeMillis();
            _lastReadTime.Set(currentMillis);
            _lastWriteTime.Set(currentMillis);

            _timer = _executor.ScheduleAtFixedRate(Heartbeating, ctx, TimeToHeartBeatMillis, TimeToHeartBeatMillis);
        }

        private void Destroy()
        {
            _state = 2;

            // .NET-specific:
            if (_timer != null)
            {
                ExecutorService.Cancel(_timer);
                _timer = null;
            }
            if (_executor != null)
            {
                _executor.Shutdown();
            }
        }

        private void Heartbeating(object state)
        {
            var ctx = (ChannelHandlerContext) state;
            if (!ctx.Channel.IsOpen)
            {
                return;
            }

            var currentTime = Convenient.CurrentTimeMillis();
            var lastIoTime = Math.Max(_lastReadTime.Get(), _lastWriteTime.Get());
            var nextDelay = TimeToHeartBeatMillis - (currentTime - lastIoTime);

            if (_peerConnection != null && nextDelay <= 0)
            {
                Logger.Debug("Sending heart beat to {0}. Channel: {1}.", _peerConnection.RemotePeer, _peerConnection.Channel);
                var builder = _pingBuilderFactory.Create();
                var taskPing = builder.SetPeerConnection(_peerConnection).Start();
                builder.NotifyAutomaticFutures(taskPing);
            }
            else
            {
                // TODO fix possible NPE
                Logger.Debug("Not sending heart beat to {0}. Channel: {1}.", _peerConnection.RemotePeer, _peerConnection.Channel);
            }
        }

        public override IChannelHandler CreateNewInstance()
        {
            return new HeartBeat(TimeToHeartBeatMillis, _pingBuilderFactory);
        }
    }
}
