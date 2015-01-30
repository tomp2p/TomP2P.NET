using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;
using TomP2P.Futures;
using TomP2P.Message;
using TomP2P.P2P;
using TomP2P.Peers;
using TomP2P.Rpc;
using Pipeline = TomP2P.Connection.Windows.Netty.Pipeline;

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
            _random = new InteropRandom((ulong)peerId.GetHashCode()); // TODO check if same results in Java
        }

        /// <summary>
        /// Sends a message via UDP.
        /// </summary>
        /// <param name="handler">The handler to deal with the response message.</param>
        /// <param name="tcsResponse">The TCS for the response message. (FutureResponse equivalent.)</param>
        /// <param name="message">The message to send.</param>
        /// <param name="channelCreator">The channel creator for the UDP channel.</param>
        /// <param name="idleUdpSeconds">The idle time of a message until fail.</param>
        /// <param name="broadcast">True, if the message is to be sent via layer 2 broadcast.</param>
        /// <returns>The response message or null, if it is fire-and-forget or a failure occurred.</returns>
        public async Task SendUdpAsync(IInboundHandler handler, TaskCompletionSource<Message.Message> tcsResponse, Message.Message message, ChannelCreator channelCreator, int idleUdpSeconds, bool broadcast)
        {
            // no need to continue if already finished
            if (tcsResponse.Task.IsCompleted)
            {
                return;
            }
            RemovePeerIfFailed(tcsResponse, message);

            bool isFireAndForget = handler == null;

            // relay options
            if (message.Sender.IsRelayed)
            {
                message.SetPeerSocketAddresses(message.Sender.PeerSocketAddresses);

                IList<PeerSocketAddress> relayAddresses = new List<PeerSocketAddress>(message.Recipient.PeerSocketAddresses);
                Logger.Debug("Send neighbor request to random relay peer {0}.", relayAddresses);
                if (relayAddresses.Count > 0)
                {
                    var relayAddress = relayAddresses[_random.NextInt(relayAddresses.Count)];
                    message.SetRecipientRelay(message.Recipient
                        .ChangePeerSocketAddress(relayAddress)
                        .ChangeIsRelayed(true));
                }
                else
                {
                    const string msg = "Peer is relayed, but no relay is given.";
                    Logger.Error(msg);
                    tcsResponse.SetException(new TaskFailedException(msg));
                    return;
                }
            }

            // check for invalid UDP connection to unreachable peers
            if (message.Recipient.IsRelayed && message.Command != Rpc.Rpc.Commands.Neighbor.GetNr()
                && message.Command != Rpc.Rpc.Commands.Ping.GetNr())
            {
                string msg =
                    String.Format(
                        "Tried to send a UDP message to unreachable peers. Only TCP messages can be sent to unreachable peers: {0}.",
                        message);
                Logger.Warn(msg);
                tcsResponse.SetException(new TaskFailedException(msg));
                return;
            }

            // pipeline handler setup
            TimeoutFactory timeoutFactory = CreateTimeoutHandler(tcsResponse, idleUdpSeconds, isFireAndForget);
            var handlers = new Dictionary<string, IChannelHandler>();
            if (!isFireAndForget)
            {
                // TODO add timeout handlers
                //handlers.Add("timeout0", timeoutFactory.CreateIdleStateHandlerTomP2P());
                //handlers.Add("timeout1", timeoutFactory.CreateTimeHandler());
            }
            handlers.Add("decoder", new TomP2PSinglePacketUdp(ChannelClientConfiguration.SignatureFactory));
            handlers.Add("encoder", new TomP2POutbound(false, ChannelClientConfiguration.SignatureFactory));
            if (!isFireAndForget)
            {
                handlers.Add("handler", handler);
            }
            
            // create UDP channel
            MyUdpClient udpClient = null;
            try
            {
                udpClient = channelCreator.CreateUdp(broadcast, handlers);
            }
            catch (Exception ex)
            {
                string msg = "Channel creation failed. " + ex;
                Logger.Debug(msg);
                tcsResponse.SetException(ex);
                // may have been closed by the other side
                // or it may have been canceled from this side
            }
            // check if channel could be created (due to shutdown)
            if (udpClient == null)
            {
                const string msg = "Could not create a UDP socket. (Due to shutdown.)";
                Logger.Warn(msg);
                tcsResponse.SetException(new TaskFailedException(msg));
                return;
            }
            Logger.Debug("About to connect to {0} with channel {1}, ff = {2}.", message.Recipient, udpClient, isFireAndForget);

            // send request message
            // processes client-side outbound pipeline
            await udpClient.SendAsync(message); // TODO await?

            // if not fire-and-forget, receive response
            if (isFireAndForget)
            {
                Logger.Debug("Fire and forget message {0} sent. Close channel {1} now.", message, udpClient);
                tcsResponse.SetResult(null); // set FF result
            }
            else
            {
                // TODO correct? or should MyUdpServer receive answer?
                // receive response message
                // processes client-side inbound pipeline
                await udpClient.ReceiveAsync();
            }
            udpClient.Close();
        }

        /*
        /// <summary>
        /// Sends a message via TCP.
        /// </summary>
        /// <param name="isFireAndFroget">True, if handler == null.</param>
        /// <param name="tcsResponse">The TCS for the response message. (FutureResponse equivalent.)</param>
        /// <param name="message">The message to send.</param>
        /// <param name="channelCreator">The channel creator for the TCP channel.</param>
        /// <param name="idleTcpSeconds">The idle time until message fail.</param>
        /// <param name="connectTimeoutMillis">The idle time for the connection setup.</param>
        /// <param name="peerConnection"></param>
        /// <returns></returns>
        public Message.Message SendTcp(bool isFireAndFroget, TaskCompletionSource<Message.Message> tcsResponse,
            Message.Message message, ChannelCreator channelCreator, int idleTcpSeconds, int connectTimeoutMillis,
            PeerConnection peerConnection)
        {
            // no need to continue if already finished
            if (tcsResponse.Task.IsCompleted)
            {
                return tcsResponse.Task.Result;
            }
            RemovePeerIfFailed(tcsResponse, message);

            // we need to set the neighbors if we use relays
            if (message.Sender.IsRelayed && message.Sender.PeerSocketAddresses.Count != 0)
            {
                message.SetPeerSocketAddresses(message.Sender.PeerSocketAddresses);
            }

            if (peerConnection != null && peerConnection.Channel != null && peerConnection.Channel.IsActive)
            {
                var channel = SendTcpPeerConnection(peerConnection, handler, channelCreator, tcs);
                //afterConnect
            }
            else if (channelCreator != null)
            {
                var timeoutHandler = CreateTimeoutHandler(tcsResponse, idleTcpSeconds, isFireAndFroget);
                // check relay
                if (message.Recipient.IsRelayed)
                {
                    // check if reverse connection is possible
                    if (!message.Sender.IsRelayed)
                    {
                        HandleRcon(handler, tcsResponse, message, channelCreator, connectTimeoutMillis, peerConnection,
                            timeoutHandler);
                    }
                    else
                    {
                        HandleRelay(handler, tcsResponse, message, channelCreator, idleTcpSeconds, connectTimeoutMillis,
                            peerConnection, timeoutHandler);
                    }
                }
                else
                {
                    // "connectAndSend"
                    var recipient = message.Recipient.CreateSocketTcp();
                    var channel = SendTcpCreateChannel(recipient, channelCreator, peerConnection, handler,
                        timeoutHandler, connectTimeoutMillis, tcsResponse);
                    AfterConnect(tcsResponse, message, channel, isFireAndFroget);
                }
            }
        }

        private MyTcpClient SendTcpCreateChannel(IPEndPoint recipient, ChannelCreator channelCreator,
            PeerConnection peerConnection, handler , TimeoutFactory timeoutHandler, int connectTimeoutMillis,
            TaskCompletionSource<Message.Message> tcsResponse)
        {
            // TODO attach handlers

            var channel = channelCreator.CreateTcp(recipient, connectTimeoutMillis, handlers, tcsResponse);
            if (peerConnection != null && channel != null)
            {
                peerConnection.SetChannel(channel);
                // TODO heartbeat
            }
            return channel;
        }

        private MyTcpClient SendTcpPeerConnection(PeerConnection peerConnection, handler , ChannelCreator channelCreator,
            TaskCompletionSource<Message.Message> tcsResponse)
        {
            throw new NotImplementedException();
            // if the channel gets closed, the future should get notified
            var channel = peerConnection.Channel;

            // channel creator can be null if we don't need to create any channels
            if (channelCreator != null)
            {
                // TODO this doesn't do anything yet
                channelCreator.SetupCloseListener(channel, tcsResponse);
            }

            // TODO the pipeline is manipulated here
            // we need to replace the handler if this comes from the peer that created a peer connection,
            // otherwise we need to add a handler
            AddOrReplace();
            // TODO uncommented Java stuff needed?
        }*/

        private void RemovePeerIfFailed(TaskCompletionSource<Message.Message> tcs, Message.Message message)
        {
            // execute the following delegate only if TCS task failed
            tcs.Task.ContinueWith(delegate(Task task)
            {
                if (message.Recipient.IsRelayed)
                {
                    // TODO: Java, make the relay go away if failed
                }
                else
                {
                    lock (_peerStatusListeners)
                    {
                        foreach (var listener in _peerStatusListeners)
                        {
                            listener.PeerFailed(message.Recipient, new PeerException(tcs));
                        }
                    }
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Creates a timeout handler or null if it is a fire and forget message.
        /// In this case, we don't expect a response and we don't need a timeout.
        /// </summary>
        /// <param name="tcsResponse">The TCS for the response message. (FutureResponse equivalent.)</param>
        /// <param name="idleMillis">The timeout.</param>
        /// <param name="isFireAndForget">True, if we don't expect a response.</param>
        /// <returns>The timeout factory that will create timeout handlers.</returns>
        private TimeoutFactory CreateTimeoutHandler(TaskCompletionSource<Message.Message> tcsResponse, int idleMillis,
            bool isFireAndForget)
        {
            return isFireAndForget ? null : new TimeoutFactory(tcsResponse, idleMillis, _peerStatusListeners, "Sender");
        }
    }
}
