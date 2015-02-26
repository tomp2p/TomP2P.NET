using System;
using System.Threading;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Connection
{
    public class IdleStateHandlerTomP2P : BaseChannelHandler, IDuplexHandler
    {
        public int AllIdleTimeMillis { get; private set; }

        private readonly VolatileLong _lastReadTime = new VolatileLong(0);
        private readonly VolatileLong _lastWriteTime = new VolatileLong(0);

        // .NET-specific
        private ExecutorService _executor;
        private volatile CancellationTokenSource _cts;

        private volatile int _state; // 0 - none, 1 - initialized, 2- destroyed

        /// <summary>
        /// Creates a new instance firing IdleStateEvents.
        /// </summary>
        /// <param name="allIdleTimeSeconds">An IdleStateEvent whose state is AllIdle will be triggered
        /// when neither read nor write was performed for the specified period of time.
        /// Specify 0 to disable.</param>
        public IdleStateHandlerTomP2P(int allIdleTimeSeconds)
        {
            if (allIdleTimeSeconds <= 0)
            {
                AllIdleTimeMillis = 0;
            }
            else
            {
                AllIdleTimeMillis = (int) TimeSpan.FromSeconds(allIdleTimeSeconds).TotalMilliseconds;
            }
        }

        public override void HandlerAdded(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.IsActive)
            {
                Initialize(ctx);
            }
        }

        public override void HandlerRemoved(ChannelHandlerContext ctx)
        {
            Destroy();
        }

        public override void ChannelActive(ChannelHandlerContext ctx)
        {
            Initialize(ctx);
        }

        public override void ChannelInactive(ChannelHandlerContext ctx)
        {
            Destroy();
        }

        public void Read(ChannelHandlerContext ctx, object msg)
        {
            _lastReadTime.Set(Convenient.CurrentTimeMillis());
            ctx.FireRead(msg);
        }

        public void Write(ChannelHandlerContext ctx, object msg)
        {
            ctx.Channel.WriteCompleted += channel => _lastWriteTime.Set(Convenient.CurrentTimeMillis());
            //ctx.FireWrite(msg); // TODO needed?
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

            if (AllIdleTimeMillis > 0)
            {
                _cts = _executor.Schedule(Callback, ctx, AllIdleTimeMillis);
            }
        }

        private void Callback(object state)
        {
            var ctx = state as ChannelHandlerContext;

            if (ctx == null || !ctx.Channel.IsOpen)
            {
                return;
            }
            long currentTime = Convenient.CurrentTimeMillis();
            long lastIoTime = Math.Max(_lastReadTime.Get(), _lastWriteTime.Get());
            long nextDelay = (AllIdleTimeMillis - (currentTime - lastIoTime));
            if (nextDelay <= 0)
            {
                // both reader and writer are idle
                // --> set a new timeout and notify the callback
                //Logger.Debug("Both reader and writer are idle...");
                _cts = _executor.Schedule(Callback, ctx, AllIdleTimeMillis);
                try
                {
                    ctx.FireUserEventTriggered(this);
                }
                catch (Exception ex)
                {
                    ctx.FireExceptionCaught(ex);
                }
            }
            else
            {
                // either read or write occurred before the timeout
                // --> set a new timeout with shorter delayMillis
                _cts = _executor.Schedule(Callback, ctx, nextDelay);
            }
        }

        private void Destroy()
        {
            _state = 2;
            // .NET-specific:
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
            if (_executor != null)
            {
                _executor.Shutdown();
            }
        }

        public void UserEventTriggered(ChannelHandlerContext ctx, object evt)
        {
            // nothing to do here
        }
    }
}
