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

            // "afterConnect"
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
            // (await for possible exception re-throw, does not block)
            await udpClient.SendAsync(message);

            // if not fire-and-forget, receive response
            if (isFireAndForget)
            {
                Logger.Debug("Fire and forget message {0} sent. Close channel {1} now.", message, udpClient);
                tcsResponse.SetResult(null); // set FF result
            }
            else
            {
                // receive response message
                // processes client-side inbound pipeline
                await udpClient.ReceiveAsync();
            }
            udpClient.Close();
        }

        /// <summary>
        /// Sends a message via TCP.
        /// </summary>
        /// <param name="handler">The handler to deal with the response message.</param>
        /// <param name="tcsResponse">The TCS for the response message. (FutureResponse equivalent.)</param>
        /// <param name="message">The message to send.</param>
        /// <param name="channelCreator">The channel creator for the TCP channel.</param>
        /// <param name="idleTcpSeconds">The idle time until message fail.</param>
        /// <param name="connectTimeoutMillis">The idle time for the connection setup.</param>
        /// <param name="peerConnection"></param>
        /// <returns></returns>
        public async Task SendTcpAsync(IInboundHandler handler, TaskCompletionSource<Message.Message> tcsResponse,
            Message.Message message, ChannelCreator channelCreator, int idleTcpSeconds, int connectTimeoutMillis,
            PeerConnection peerConnection)
        {
            // no need to continue if already finished
            if (tcsResponse.Task.IsCompleted)
            {
                return;
            }
            RemovePeerIfFailed(tcsResponse, message);

            bool isFireAndForget = handler == null;

            // we need to set the neighbors if we use relays
            if (message.Sender.IsRelayed && message.Sender.PeerSocketAddresses.Count != 0)
            {
                message.SetPeerSocketAddresses(message.Sender.PeerSocketAddresses);
            }

            ITcpChannel tcpClient;
            if (peerConnection != null && peerConnection.Channel != null && peerConnection.Channel.IsActive)
            {
                tcpClient = SendTcpPeerConnection(peerConnection, handler, channelCreator, tcsResponse);
                if (AfterConnect(tcsResponse, message, tcpClient, isFireAndForget))
                {
                    // only send if channel could be created
                    // TODO merge SendAsync(message) into BaseChannel and create a unified send method here
                    // send request message
                    // processes client-side outbound pipeline
                    // (await for possible exception re-throw, does not block)
                    await tcpClient.SendMessageAsync(message);

                    // if not fire-and-forget, receive response
                    if (isFireAndForget)
                    {
                        Logger.Debug("Fire and forget message {0} sent. Close channel {1} now.", message, tcpClient);
                        tcsResponse.SetResult(null); // set FF result
                    }
                    else
                    {
                        // receive response message
                        // processes client-side inbound pipeline
                        await udpClient.ReceiveAsync();
                    }
                    udpClient.Close();
                }
            }
            else if (channelCreator != null)
            {
                var timeoutHandler = CreateTimeoutHandler(tcsResponse, idleTcpSeconds, handler == null);
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

        /// <summary>
        /// .NET implementation of afterConnect(). Works somewhat different because the following
        /// sending operations differ.
        /// </summary>
        /// <param name="tcsResponse"></param>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        /// <param name="isFireAndForget"></param>
        /// <returns>True, if channel could be established. False, otherwise.</returns>
        private void AfterConnect(TaskCompletionSource<Message.Message> tcsResponse, Message.Message message,
            IChannel channel, bool isFireAndForget)
        {
            // TODO use for UDP connections, too
            // TODO find clean-mechanism to show the channel-creation fails (UDP uses try/catch)
            // check if channel could be created (due to shutdown)
            if (channel == null)
            {
                string msg = String.Format("Could not create a {0} socket. (Due to shutdown.)", message.IsUdp ? "UDP" : "TCP");
                Logger.Warn(msg);
                tcsResponse.SetException(new TaskFailedException(msg));
                return;
            }
            Logger.Debug("About to connect to {0} with channel {1}, ff = {2}.", message.Recipient, channel, isFireAndForget);
            
            // sending
            var sendTask = channel.SendMessageAsync(message);
            AfterSend(sendTask, tcsResponse, isFireAndForget, channel);
        }

        /// <summary>
        /// After sending, we check if the write was successful or if it was a fire-and-forget.
        /// </summary>
        /// <param name="sendTask">The task of the send operation.</param>
        /// <param name="tcsResponse"></param>
        /// <param name="isFireAndForget">True, if we don't expect a response message.</param>
        /// <param name="channel"></param>
        private void AfterSend(Task sendTask, TaskCompletionSource<Message.Message> tcsResponse, bool isFireAndForget, IChannel channel)
        {
            // TODO use for UDP connections, too
            sendTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    string msg = String.Format("Failed to write channel the request {0} {1}.", tcsResponse.Task.AsyncState,
                        sendTask.Exception);
                    Logger.Warn(msg);
                    if (t.Exception != null)
                    {
                        tcsResponse.SetException(t.Exception);
                    }
                    else
                    {
                        tcsResponse.SetException(new TaskFailedException(msg));
                    }
                }
                if (isFireAndForget)
                {
                    Logger.Debug("Fire and forget message {0} sent. Close channel {1} now. {0}", tcsResponse.Task.AsyncState, channel);
                    tcsResponse.SetResult(null); // set FF result
                    // close channel now
                    channel.Close();
                }
            });
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

        private ITcpChannel SendTcpPeerConnection(PeerConnection peerConnection, IChannelHandler handler , ChannelCreator channelCreator,
            TaskCompletionSource<Message.Message> tcsResponse)
        {
            // if the channel gets closed, the future should get notified
            var channel = peerConnection.Channel;

            // channel creator can be null if we don't need to create any channels
            if (channelCreator != null)
            {
                // TODO this doesn't do anything yet
                channelCreator.SetupCloseListener(channel, tcsResponse);
            }

            // we need to replace the handler if this comes from the peer that created a peer connection,
            // otherwise we need to add a handler
            AddOrReplace(channel.Pipeline, "dispatcher", "handler", handler);
            // TODO uncommented Java stuff needed?
            return channel;
        }

        private void AddOrReplace(Pipeline pipeline, string before, string name, IChannelHandler handler)
        {
            if (pipeline.Names.Contains(name))
            {
                pipeline.Replace(name, name, handler);
            }
            else
            {
                if (before == null)
                {
                    pipeline.AddFirst(name, handler);
                }
                else
                {
                    pipeline.AddBefore(before, name, handler);
                }
            }
        }

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
