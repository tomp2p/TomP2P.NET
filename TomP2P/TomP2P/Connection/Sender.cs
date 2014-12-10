using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty.Transport;
using TomP2P.Futures;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    /// <summary>
    /// The class that sends out messages.
    /// </summary>
    public class Sender
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IList<IPeerStatusListener> _peerStatusListeners;
        public ChannelClientConfiguration ChannelClientConfiguration { get; private set; }
        private readonly Dispatcher _dispatcher;
        private readonly InteropRandom _random;

        // this map caches all messages which are meant to be sent by a reverse connection setup
        private readonly ConcurrentDictionary<int, FutureResponse> _cachedRequests = new ConcurrentDictionary<int, FutureResponse>();

        public IPingBuilderFactory PingBuilderFactory { get; private set; }

        public Sender(Number160 peerId, IList<IPeerStatusListener> peerStatusListeners,
            ChannelClientConfiguration channelClientConfiguration, Dispatcher dispatcher)
        {
            _peerStatusListeners = peerStatusListeners;
            ChannelClientConfiguration = channelClientConfiguration;
            _dispatcher = dispatcher;
            _random = new InteropRandom((ulong) peerId.GetHashCode()); // TODO check if same results in Java
        }

        public Sender SetPingBuilderFactory(IPingBuilderFactory pingBuilderFactory)
        {
            PingBuilderFactory = pingBuilderFactory;
            return this;
        }

        // TODO Java uses Netty's SimpleChannelInboundHandler
        /// <summary>
        /// Sends a message via TCP.
        /// </summary>
        /// <param name="handler">The handler to deal with a reply message.</param>
        /// <param name="futureResponse">The future to set the response.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="channelCreator">The channel creator for the UDP channel.</param>
        /// <param name="idleTcpSeconds">The idle time until message fail.</param>
        /// <param name="connectTimeoutMillis">The idle time fot the connection setup.</param>
        /// <param name="peerConnection"></param>
        public void SendTcp(SimpleChannelInboundHandler<Message.Message> handler, FutureResponse futureResponse, Message.Message message, ChannelCreator channelCreator,
            int idleTcpSeconds, int connectTimeoutMillis, PeerConnection peerConnection)
        {
            // no need to continue if we already finished
            if (futureResponse.IsCompleted())
            {
                return;
            }
            RemovePeerIfFailed(futureResponse, message);

            // we need to set the neighbors if we use relays
            if (message.Sender.IsRelayed && message.Sender.PeerSocketAddresses.Count != 0)
            {
                message.SetPeerSocketAddresses(message.Sender.PeerSocketAddresses);
            }

            IChannelFuture channelFuture;
            if (peerConnection != null && peerConnection.ChannelFuture() != null
                && peerConnection.ChannelFuture().Channel().IsActive())
            {
                channelFuture = SendTcpPeerConnection(peerConnection, handler, channelCreator, futureResponse);
                AfterConnect(futureResponse, message, channelFuture, handler == null);
            }

            throw new NotImplementedException();
        }

        private void RemovePeerIfFailed(FutureResponse futureResponse, Message.Message message)
        {
            throw new NotImplementedException();
        }

        private IChannelFuture SendTcpPeerConnection(PeerConnection peerConnection, IChannelHandler handler,
            ChannelCreator channelCreator, FutureResponse futureResponse)
        {
            // if the channel gets closed, the future should get notified
            IChannelFuture channelFuture = peerConnection.ChannelFuture();

            // channelCreater can be null if we don't need to create any channels
            if (channelCreator != null)
            {
                channelCreator.SetupCloseListener(channelFuture, futureResponse);
            }
            IChannelPipeline pipeline = channelFuture.Channel().Pipeline();

            // we need to replace the handler if this comes from the peer that
            // created a peerConnection, otherwise we need to add a handler
            AddOrReplace(pipeline, "dispatcher", "handler", handler);

            // TODO uncomments needed?
            return channelFuture;
        }

        private bool AddOrReplace(IChannelPipeline pipeline, string before, string name, IChannelHandler channelHandler)
        {
            IList<string> names = pipeline.Names();
            if (names.Contains(name))
            {
                pipeline.Replace(name, name, channelHandler);
                return false;
            }
            else
            {
                if (before == null)
                {
                    pipeline.AddFirst(name, channelHandler);
                }
                else
                {
                    pipeline.AddBefore(before, name, channelHandler);
                }
                return true;
            }
        }

        private void AfterConnect(FutureResponse futureResponse, Message.Message message, IChannelFuture channelFuture,
            bool fireAndForget)
        {
            if (channelFuture == null)
            {
                futureResponse.Failed("Could not create a " + (message.IsUdp ? "UDP" : "TCP") + " channel.");
                return;
            }
            Logger.Debug("About to connect to {0} with channel {1}, ff={2}.", message.Recipient, channelFuture.Channel(), fireAndForget);
            ICancel connectCancel = CreateCancel(channelFuture);
            
            // TODO implement rest of this method
            throw new NotImplementedException();
        }

        private static ICancel CreateCancel(IChannelFuture channelFuture)
        {
            return new AnonymousCancel(channelFuture);
        }

        private class AnonymousCancel : ICancel
        {
            private readonly IChannelFuture _channelFuture;
            public AnonymousCancel(IChannelFuture channelFuture)
            {
                _channelFuture = channelFuture;
            }

            public void Cancel()
            {
                _channelFuture.Cancel(true);
            }
        }
    }
}
