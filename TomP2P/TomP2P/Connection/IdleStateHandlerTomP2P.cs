using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Connection
{
    /// <summary>
    /// Stripped-down version of the <see cref="IdleStateHandler"/>.
    /// </summary>
    public class IdleStateHandlerTomP2P : BaseChannelHandler, IDuplexHandler
    {
        public int AllIdleTimeMillis { get; private set; }

        private VolatileLong _lastReadTime;

        private VolatileLong _lastWriteTime;

        private volatile Task _allIdleTimeoutTask;
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
                AllIdleTimeMillis = TimeSpan.FromSeconds(allIdleTimeSeconds).Milliseconds;
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
            // make channel.write async -> attach listener
            throw new NotImplementedException();

            ctx.FireWrite(msg);
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
            var currentMillis = Convenient.CurrentTimeMillis();
            _lastReadTime.Set(currentMillis);
            _lastWriteTime.Set(currentMillis);

            if (AllIdleTimeMillis > 0)
            {
                // one-shot task
                StartAllIdleTimeoutTask(ctx, AllIdleTimeMillis);
            }
        }

        // use "async void" because it's an event-like task
        private async void StartAllIdleTimeoutTask(ChannelHandlerContext ctx, long delay)
        {
            _cts = new CancellationTokenSource();
            _allIdleTimeoutTask = Task.Delay(AllIdleTimeMillis, _cts.Token);

            await _allIdleTimeoutTask;

            // continue with "AllIdleTimeoutTask"
            if (!ctx.Channel.IsOpen)
            {
                return;
            }
            var currentTime = Convenient.CurrentTimeMillis();
            var lastIoTime = Math.Max(_lastReadTime.Get(), _lastWriteTime.Get());
            var nextDelay = AllIdleTimeMillis - (currentTime - lastIoTime);
            if (nextDelay <= 0)
            {
                // both reader and writer are idle
                // --> set a new timeout and notify the callback
                StartAllIdleTimeoutTask(ctx, AllIdleTimeMillis);
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
                // --> set a new timeout with shorter delay
                StartAllIdleTimeoutTask(ctx, nextDelay);
            }
        }

        private void Destroy()
        {
            _state = 2;
            if (_allIdleTimeoutTask != null)
            {
                _cts.Cancel();
                _allIdleTimeoutTask = null;
                _cts = null;
            }
        }

        public void UserEventTriggered(ChannelHandlerContext ctx, object evt)
        {
            // nothing to do here
        }
    }
}
