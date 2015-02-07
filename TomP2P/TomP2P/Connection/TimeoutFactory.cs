using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    /// <summary>
    /// A factory that creates timeout handlers.
    /// </summary>
    public class TimeoutFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly TaskCompletionSource<Message.Message> _tcsResponse;
        private readonly int _timeoutSeconds;
        private readonly IList<IPeerStatusListener> _peerStatusListeners;
        private readonly string _name;

        /// <summary>
        /// Creates a factory for timeout handlers.
        /// </summary>
        /// <param name="tcsResponse">The TCS for the response message. (FutureResponse equivalent.)</param>
        /// <param name="timeoutSeconds">The time for a timeout.</param>
        /// <param name="peerStatusListeners">The listeners that get notified when a timeout happens.</param>
        /// <param name="name"></param>
        public TimeoutFactory(TaskCompletionSource<Message.Message> tcsResponse, int timeoutSeconds,
            IList<IPeerStatusListener> peerStatusListeners, string name)
        {
            _tcsResponse = tcsResponse;
            _timeoutSeconds = timeoutSeconds;
            _peerStatusListeners = peerStatusListeners;
            _name = name;
        }

        public IChannelHandler CreateIdleStateHandlerTomP2P()
        {
            return new IdleStateHandlerTomP2P(_timeoutSeconds);
        }

        public IChannelHandler CreateTimeHandler()
        {
            return new TimeHandler(_tcsResponse, _peerStatusListeners, _name);
        }

        public static void RemoveTimeout(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.Pipeline.Names.Contains("timeout0"))
            {
                ctx.Channel.Pipeline.Remove("timeout0");
            }
            if (ctx.Channel.Pipeline.Names.Contains("timeout1"))
            {
                ctx.Channel.Pipeline.Remove("timeout1");
            }
        }

        /// <summary>
        /// The timeout handler that gets called from the <see cref="IdleStateHandler"/>
        /// </summary>
        private class TimeHandler : BaseChannelHandler, IDuplexHandler
        {
            private readonly TaskCompletionSource<Message.Message> _tcsResponse;
            private readonly IList<IPeerStatusListener> _peerStatusListeners;
            private readonly string _name;

            public TimeHandler(TaskCompletionSource<Message.Message> tcsResponse,
                IList<IPeerStatusListener> peerStatusListeners, string name)
            {
                _tcsResponse = tcsResponse;
                _peerStatusListeners = peerStatusListeners;
                _name = name;
            }

            // TODO when and where shall this get called?
            public void UserEventTriggered(ChannelHandlerContext ctx, object evt)
            {
                if (evt is IdleStateHandlerTomP2P)
                {
                    Logger.Warn("Channel timeout for channel {0} {1}.", _name, ctx.Channel);
                    PeerAddress recipient;
                    if (_tcsResponse != null)
                    {
                        var requestMessage = (Message.Message) _tcsResponse.Task.AsyncState;

                        Logger.Warn("Request status is {0}.", requestMessage);
                        ctx.Channel.Closed +=
                            channel => _tcsResponse.SetException(new TaskFailedException("Channel is idle " + evt));
                        ctx.Channel.Close();

                        recipient = requestMessage.Recipient;
                    }
                    else
                    {
                        ctx.Close();
                        // check if we have set an attribute at least
                        // (if we have already decoded the header)

                    }
                }
            }

            public void Read(ChannelHandlerContext ctx, object msg)
            {
                // nothing to read here
            }

            public void Write(ChannelHandlerContext ctx, object msg)
            {
                // nothing to write here
            }
        }
    }
}
