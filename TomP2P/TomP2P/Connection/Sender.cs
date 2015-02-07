using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;
using TomP2P.Futures;
using TomP2P.Message;
using TomP2P.P2P;
using TomP2P.Peers;
using TomP2P.Rpc;

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
        private readonly ConcurrentDictionary<int, TaskCompletionSource<Message.Message>> _cachedRequests = new ConcurrentDictionary<int, TaskCompletionSource<Message.Message>>();

        public IPingBuilderFactory PingBuilderFactory { get; private set; }

        public Sender(Number160 peerId, IList<IPeerStatusListener> peerStatusListeners,
            ChannelClientConfiguration channelClientConfiguration, Dispatcher dispatcher)
        {
            _peerStatusListeners = peerStatusListeners;
            ChannelClientConfiguration = channelClientConfiguration;
            _dispatcher = dispatcher;
            _random = new InteropRandom((ulong)peerId.GetHashCode()); // TODO check if same results in Java
        }

        public Sender SetPingBuilderFactory(IPingBuilderFactory pingBuilderFactory)
        {
            PingBuilderFactory = pingBuilderFactory;
            return this;
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
            await udpClient.SendMessageAsync(message);

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
                await udpClient.ReceiveMessageAsync();
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
            if (peerConnection != null && peerConnection.Channel != null && peerConnection.Channel.IsActive)
            {
                var channel = SendTcpPeerConnection(peerConnection, handler, channelCreator, tcsResponse);
                await AfterConnectAsync(tcsResponse, message, channel, isFireAndForget);
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
                        await HandleRconAsync(handler, tcsResponse, message, channelCreator, connectTimeoutMillis, peerConnection,
                            timeoutHandler);
                    }
                    else
                    {
                        await HandleRelayAsync(handler, tcsResponse, message, channelCreator, idleTcpSeconds, connectTimeoutMillis,
                            peerConnection, timeoutHandler);
                    }
                }
                // normal connection
                else
                {
                    await ConnectAndSendAsync(handler, tcsResponse, channelCreator, connectTimeoutMillis, peerConnection,
                        timeoutHandler, message);
                }
            }
        }

        /// <summary>
        /// This method initiates the reverse connection setup.
        /// It creates a new message and sends it via relay to the unreachable peer
        /// which then connects to this peer again. After the connect message from the
        /// unreachable peer, this peer will send the original message and its content
        /// directly.
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="tcsResponse"></param>
        /// <param name="message"></param>
        /// <param name="channelCreator"></param>
        /// <param name="connectTimeoutMillis"></param>
        /// <param name="peerConnection"></param>
        /// <param name="timeoutHandler"></param>
        private async Task HandleRconAsync(IInboundHandler handler, TaskCompletionSource<Message.Message> tcsResponse,
            Message.Message message, ChannelCreator channelCreator, int connectTimeoutMillis,
            PeerConnection peerConnection, TimeoutFactory timeoutHandler)
        {
            message.SetKeepAlive(true);

            Logger.Debug("Initiate reverse connection setup to peer with address {0}.", message.Recipient);
            var rconMessage = CreateRconMessage(message);

            // TODO works?
            // cache the original message until the connection is established
            _cachedRequests.AddOrUpdate(message.MessageId, tcsResponse, (i, source) => tcsResponse);

            // wait for response (whether the reverse connection setup was successful)
            var tcsRconResponse = new TaskCompletionSource<Message.Message>(rconMessage);

            // .NET-specific: specify and use a RconInboundHandler class
            var rconInboundHandler = new RconInboundHandler(tcsRconResponse, tcsResponse);

            // send reverse connection request instead of normal message
            await SendTcpAsync(rconInboundHandler, tcsRconResponse, rconMessage, channelCreator, connectTimeoutMillis,
                connectTimeoutMillis, peerConnection);
        }

        /// <summary>
        /// This method makes a copy of the original message and prepares it for 
        /// sending it to the relay.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static Message.Message CreateRconMessage(Message.Message message)
        {
            // get relay address from the unreachable peer
            var relayAddresses = message.Recipient.PeerSocketAddresses.ToArray();
            PeerSocketAddress socketAddress = null;

            if (relayAddresses.Length > 0)
            {
                // we should be fair and choose one of the relays randomly
                socketAddress = relayAddresses[Utils.Utils.RandomPositiveInt(relayAddresses.Length)];
            }
            else
            {
                throw new ArgumentException("There are no PeerSocketAddresses available for this relayed peer. This should not be possible!");
            }

            // we need to make a copy of the original message
            var rconMessage = new Message.Message();
            rconMessage.SetSender(message.Sender);
            rconMessage.SetVersion(message.Version);
            rconMessage.SetIntValue(message.MessageId);

            // make the message ready to send
            PeerAddress recipient =
                message.Recipient
                    .ChangeAddress(socketAddress.InetAddress)
                    .ChangePorts(socketAddress.TcpPort, socketAddress.UdpPort)
                    .ChangeIsRelayed(false);
            rconMessage.SetRecipient(recipient);
            rconMessage.SetCommand(Rpc.Rpc.Commands.Rcon.GetNr());
            rconMessage.SetType(Message.Message.MessageType.Request1);

            return rconMessage;
        }

        /// <summary>
        /// // TODO document
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="tcsResponse"></param>
        /// <param name="message"></param>
        /// <param name="channelCreator"></param>
        /// <param name="idleTcpSeconds"></param>
        /// <param name="connectTimeoutMillis"></param>
        /// <param name="peerConnection"></param>
        /// <param name="timeoutHandler"></param>
        private async Task HandleRelayAsync(IInboundHandler handler, TaskCompletionSource<Message.Message> tcsResponse,
            Message.Message message, ChannelCreator channelCreator, int idleTcpSeconds, int connectTimeoutMillis,
            PeerConnection peerConnection, TimeoutFactory timeoutHandler)
        {
            var taskPingDone = PingFirst(message.Recipient.PeerSocketAddresses);
            await taskPingDone;
            if (!taskPingDone.IsFaulted)
            {
                var recipient = PeerSocketAddress.CreateSocketTcp(taskPingDone.Result);
                var channel = SendTcpCreateChannel(recipient, channelCreator, peerConnection, handler,
                    timeoutHandler, connectTimeoutMillis);
                await AfterConnectAsync(tcsResponse, message, channel, handler == null);

                // TODO add this before AfterConnect?
                var taskResponse = tcsResponse.Task;
                await taskResponse;
                if (taskResponse.IsFaulted)
                {
                    if (taskResponse.Result != null &&
                        taskResponse.Result.Type != Message.Message.MessageType.User1)
                    {
                        // "clearInactivePeerSocketAddress"
                        var tmp = new List<PeerSocketAddress>();
                        foreach (var psa in message.Recipient.PeerSocketAddresses)
                        {
                            if (psa != null)
                            {
                                if (!psa.Equals(taskPingDone.Result))
                                {
                                    tmp.Add(psa);
                                }
                            }
                        }
                        message.SetPeerSocketAddresses(tmp);

                        await SendTcpAsync(handler, tcsResponse, message, channelCreator, idleTcpSeconds,
                            connectTimeoutMillis, peerConnection);
                    }
                }
            }
            else
            {
                // .NET-specific:
                tcsResponse.SetException(new TaskFailedException("No relay could be contacted. <-> " + taskPingDone.Exception));
            }
        }

        /// <summary>
        /// Ping all relays of the receiver. The first one answering is picked 
        /// as the responsible relay for this message.
        /// </summary>
        /// <param name="peerSocketAddresses">A collection of relay addresses.</param>
        /// <returns></returns>
        private Task<PeerSocketAddress> PingFirst(IEnumerable<PeerSocketAddress> peerSocketAddresses)
        {
            var tcsDone = new TaskCompletionSource<PeerSocketAddress>();

            var socketAddresses = peerSocketAddresses as PeerSocketAddress[] ?? peerSocketAddresses.ToArray();
            var forks = new Task<PeerAddress>[socketAddresses.Count()];
            int index = 0;
            foreach (var psa in socketAddresses)
            {
                if (psa != null)
                {
                    var socketAddress = PeerSocketAddress.CreateSocketUdp(psa);
                    var pingBuilder = PingBuilderFactory.Create();
                    forks[index++] = pingBuilder
                        .SetInetAddress(socketAddress.Address)
                        .SetPort(socketAddress.Port)
                        .Start(); // TODO TCS or Task needed?
                }
            }

            var ffk = new TaskForkJoin<Task<PeerAddress>>(1, true, new VolatileReferenceArray<Task<PeerAddress>>(forks));
            ffk.Task.ContinueWith(tfj =>
            {
                if (!tfj.IsFaulted)
                {
                    tcsDone.SetResult(ffk.First.Result.PeerSocketAddress);
                }
                else
                {
                    if (tfj.Exception != null)
                    {
                        tcsDone.SetException(tfj.Exception);
                    }
                    else
                    {
                        tcsDone.SetException(new TaskFailedException("TaskForkJoin<Task<PeerAddress>> failed."));
                    }
                }
            });

            return tcsDone.Task;
        }

        private ITcpClientChannel SendTcpPeerConnection(PeerConnection peerConnection, IChannelHandler handler, ChannelCreator channelCreator,
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
            return channel as ITcpClientChannel; // TODO this will fail if its a server channel!!!
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

        private async Task ConnectAndSendAsync(IInboundHandler handler, TaskCompletionSource<Message.Message> tcsResponse, ChannelCreator channelCreator, int connectTimeoutMillis, PeerConnection peerConnection, TimeoutFactory timeoutHandler, Message.Message message)
        {
            var recipient = message.Recipient.CreateSocketTcp();
            var channel = SendTcpCreateChannel(recipient, channelCreator, peerConnection, handler, timeoutHandler,
                connectTimeoutMillis);
            await AfterConnectAsync(tcsResponse, message, channel, handler == null);
        }

        private async Task AfterConnectAsync(TaskCompletionSource<Message.Message> tcsResponse, Message.Message message,
            IClientChannel channel, bool isFireAndForget)
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
            await AfterSendAsync(sendTask, tcsResponse, isFireAndForget, channel);
        }

        /// <summary>
        /// After sending, we check if the write was successful or if it was a fire-and-forget.
        /// </summary>
        /// <param name="sendTask">The task of the send operation.</param>
        /// <param name="tcsResponse"></param>
        /// <param name="isFireAndForget">True, if we don't expect a response message.</param>
        /// <param name="channel"></param>
        private async Task AfterSendAsync(Task sendTask, TaskCompletionSource<Message.Message> tcsResponse, bool isFireAndForget, IClientChannel channel)
        {
            // TODO use for UDP connections, too
            await sendTask;
            if (sendTask.IsFaulted)
            {
                string msg = String.Format("Failed to write channel the request {0} {1}.", tcsResponse.Task.AsyncState,
                    sendTask.Exception);
                Logger.Warn(msg);
                if (sendTask.Exception != null)
                {
                    tcsResponse.SetException(sendTask.Exception);
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
            }
            else
            {
                //.NET specific, we wait here for the response
                // receive response message
                // processes client-side inbound pipeline
                await channel.ReceiveMessageAsync();
            }
            channel.Close();
        }

        private ITcpClientChannel SendTcpCreateChannel(IPEndPoint recipient, ChannelCreator channelCreator,
            PeerConnection peerConnection, IChannelHandler handler, TimeoutFactory timeoutHandler, int connectTimeoutMillis)
        {
            // create pipeline
            var handlers = new Dictionary<string, IChannelHandler>();
            if (timeoutHandler != null)
            {
                // TODO add timeout handlers
                //handlers.Add("timeout0", timeoutHandler.CreateIdleStateHandlerTomP2P());
                //handlers.Add("timeout1", timeoutHandler.CreateTimeHandler());
            }
            handlers.Add("decoder", new TomP2PCumulationTcp(ChannelClientConfiguration.SignatureFactory));
            handlers.Add("encoder", new TomP2POutbound(false, ChannelClientConfiguration.SignatureFactory));
            if (peerConnection != null)
            {
                // we expect responses on this connection
                handlers.Add("dispatcher", _dispatcher);
            }
            if (timeoutHandler != null)
            {
                handlers.Add("handler", handler);
            }
            HeartBeat heartBeat = null;
            if (peerConnection != null)
            {
                heartBeat = new HeartBeat(peerConnection.HeartBeatMillis, PingBuilderFactory);
                handlers.Add("heartbeat", heartBeat);
            }

            var channel = channelCreator.CreateTcp(recipient, connectTimeoutMillis, handlers);

            if (peerConnection != null && channel != null)
            {
                peerConnection.SetChannel(channel);
                heartBeat.SetPeerConnection(peerConnection);
            }
            return channel;
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
