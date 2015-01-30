using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Message;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.Connection
{
    /// <summary>
    /// Used to deliver incoming REQUEST messages to their specific handlers.
    /// Handlers can be registered using the RegisterIoHandler function.
    /// <para>
    /// (You probably want to add an instance of this class to the end of a pipeline to be able to receive messages.
    /// This class is able to cover several channels but only one P2P network!)
    /// </para>
    /// </summary>
    public class Dispatcher : IInboundHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private readonly int _p2pId;
        private readonly PeerBean _peerBeanMaster;
        private readonly int _heartBeatMillis;

        /* Copy on write map. The Number320 key can be divided into two parts:
         * - first Number160 is the peer ID that registers
         * - second Number160 is the peer ID for which the IO handler is registered
         * For example, a relay peer can register a handler on behalf of another peer.
         * */
        private volatile IDictionary<Number320, IDictionary<int, DispatchHandler>> _ioHandlers = new Dictionary<Number320, IDictionary<int, DispatchHandler>>();

        /// <summary>
        /// Creates a dispatcher.
        /// </summary>
        /// <param name="p2pId">The P2P ID the dispatcher is looking for incoming messages.</param>
        /// <param name="peerBeanMaster"></param>
        /// <param name="heartBeatMillis"></param>
        public Dispatcher(int p2pId, PeerBean peerBeanMaster, int heartBeatMillis)
        {
            _p2pId = p2pId;
            _peerBeanMaster = peerBeanMaster;
            _heartBeatMillis = heartBeatMillis;
        }

        /// <summary>
        /// Registers a handler with this dispatcher. Future received messages adhering to the given parameters will be
        /// forwarded to that handler. Note that the dispatcher only handles REQUEST messages. This method is thread-safe,
        /// and uses copy on write as it is expected to run this only during initialization.
        /// </summary>
        /// <param name="peerId">Specifies the receiver the dispatcher filters for. This allows to use one dispatcher for 
        /// several interfaces or even nodes.</param>
        /// <param name="onBehalfOf">The IO Handler can be registered for the own use in behalf of another peer.
        /// (E.g., in case of a relay node.)</param>
        /// <param name="ioHandler">The handler which should process the given type of messages.</param>
        /// <param name="names">The command of the Message the given handler processes. All messages having that command will
        /// be forwarded to the given handler.
        /// Note: If you register multiple handlers with the same command, only the last registered handler will receive 
        /// these messages!</param>
        public void RegisterIOHandler(Number160 peerId, Number160 onBehalfOf, DispatchHandler ioHandler,
            params int[] names)
        {
            IDictionary<Number320, IDictionary<int, DispatchHandler>> copy = new Dictionary<Number320, IDictionary<int, DispatchHandler>>(_ioHandlers);
            IDictionary<int, DispatchHandler> types;
            
            // .NET specific
            copy.TryGetValue(new Number320(peerId, onBehalfOf), out types);
            if (types == null)
            {
                types = new Dictionary<int, DispatchHandler>();
                copy.Add(new Number320(peerId, onBehalfOf), types);
            }
            foreach (int name in names)
            {
                types.Put(name, ioHandler);
            }
            _ioHandlers = new ReadOnlyDictionary<Number320, IDictionary<int, DispatchHandler>>(copy);
        }

        /// <summary>
        /// If we shutdown, we remove the handlers. This means that a server may respond that the handler is unknown.
        /// </summary>
        /// <param name="peerId">The ID of the peer to remove the handlers.</param>
        /// <param name="onBehalfOf">The IO Handler can be registered for the own use in behalf of another peer.
        /// (E.g., in case of a relay node.)</param>
        public void RemoveIOHandlers(Number160 peerId, Number160 onBehalfOf)
        {
            IDictionary<Number320, IDictionary<int, DispatchHandler>> copy = new Dictionary<Number320, IDictionary<int, DispatchHandler>>(_ioHandlers);
            copy.Remove(new Number320(peerId, onBehalfOf));
            _ioHandlers = new ReadOnlyDictionary<Number320, IDictionary<int, DispatchHandler>>(copy);
        }

        public void Read(ChannelHandlerContext ctx, object msg)
        {
            // server-side:
            // message comes (over network) from sender
            // -> correct DispatchHandler handles response

            // Java uses a SimpleChannelInboundHandler that only expects Message objects
            var requestMessage = msg as Message.Message;
            if (requestMessage == null)
            {
                return;
            }
            
            Logger.Debug("Received request message {0} from channel {1}.", requestMessage, ctx.Channel);
            if (requestMessage.Version != _p2pId)
            {
                Logger.Error("Wrong version. We are looking for {0}, but we got {1}. Received: {2}.", _p2pId, requestMessage.Version, requestMessage);
                ctx.Close(); // TODO correct?
                lock (_peerBeanMaster.PeerStatusListeners)
                {
                    foreach (IPeerStatusListener listener in _peerBeanMaster.PeerStatusListeners)
                    {
                        listener.PeerFailed(requestMessage.Sender,
                            new PeerException(PeerException.AbortCauseEnum.PeerError, "Wrong P2P version."));
                    }
                }
                return;
            }
            if (!requestMessage.IsRequest())
            {
                Logger.Debug("Handing request message to the next handler. {0}", requestMessage);
                ctx.FireRead(msg);
                return;
            }

            IResponder responder = new DirectResponder(this, _peerBeanMaster, ctx, requestMessage);
            DispatchHandler myHandler = AssociatedHandler(requestMessage);
            if (myHandler != null)
            {
                bool isUdp = ctx.Channel.IsUdp;
                bool isRelay = requestMessage.Sender.IsRelayed;
                if (!isRelay && requestMessage.PeerSocketAddresses.Count != 0)
                {
                    PeerAddress sender =
                        requestMessage.Sender.ChangePeerSocketAddresses(requestMessage.PeerSocketAddresses);
                    requestMessage.SetSender(sender);
                }
                Logger.Debug("About to respond to request message {0}.", requestMessage);
                var peerConnection = new PeerConnection(requestMessage.Sender, ctx.Channel, _heartBeatMillis);

                // handle the request message
                myHandler.ForwardMessage(requestMessage, isUdp ? null : peerConnection, responder);
            }
            else
            {
                // do better error handling
                // if a handler is not present at all, print a warning
                if (_ioHandlers.Count == 0)
                {
                    Logger.Debug("No handler found for request message {0}. This peer has probably been shut down.", requestMessage);
                }
                else
                {
                    var knownCommands = KnownCommands();
                    if (knownCommands.Contains(Convert.ToInt32(requestMessage.Command)))
                    {
                        var sb = new StringBuilder();
                        foreach (int cmd in knownCommands)
                        {
                            sb.Append((Rpc.Rpc.Commands) cmd + "; ");
                        }
                        Logger.Warn("No handler found for request message {0}. Is the RPC command {1} registered? Found registered: {2}.", requestMessage, (Rpc.Rpc.Commands) requestMessage.Command, sb);
                    }
                    else
                    {
                        Logger.Debug("No handler found for request message {0}. This peer has probably been partially shut down.", requestMessage);
                    }
                }

                // return response that states that no handler was found
                var responseMessage = DispatchHandler.CreateResponseMessage(requestMessage,
                    Message.Message.MessageType.UnknownId, _peerBeanMaster.ServerPeerAddress);

                Respond(ctx, responseMessage);
            }
        }

        private IEnumerable<int> KnownCommands()
        {
            ISet<int> commandSet = new HashSet<int>();
            foreach (var entry in _ioHandlers)
            {
                commandSet.UnionWith(entry.Value.Keys);
            }
            return commandSet;
        }

        /// <summary>
        /// Responds within a session. Keeps the connection open if told to do so.
        /// Connection is only kept alive for TCP content.
        /// </summary>
        /// <param name="ctx">The channel context.</param>
        /// <param name="responseMessage">The response message to send.</param>
        internal void Respond(ChannelHandlerContext ctx, Message.Message responseMessage)
        {
            if (ctx.Channel is IUdpChannel)
            {
                // Check, if channel is still open. If not, then do not send anything
                // because this will cause an exception that will be logged.
                var channel = ctx.Channel as IUdpChannel;
                if (!channel.IsOpen)
                {
                    Logger.Debug("Channel UDP is not open. Do not reply {0}.", responseMessage);
                    return;
                }
                Logger.Debug("Response UDP message {0}.", responseMessage);
            }
            else if (ctx.Channel is ITcpChannel)
            {
                // Check, if channel is still open. If not, then do not send anything
                // because this will cause an exception that will be logged.
                var channel = ctx.Channel as ITcpChannel;
                if (!channel.IsActive)
                {
                    Logger.Debug("Channel TCP is not open. Do not reply {0}.", responseMessage);
                    return;
                }
                Logger.Debug("Response TCP message {0} to {1}.", responseMessage, ctx.Channel.Socket.RemoteEndPoint);
            }
            ctx.FireRead(responseMessage); // TODO correct?
        }

        /// <summary>
        /// Returns the registered handler for the provided message, if any.
        /// </summary>
        /// <param name="message">The message a handler should be found for.</param>
        /// <returns>The handler for the provided message or null, if none has been registered for that message.</returns>
        public DispatchHandler AssociatedHandler(Message.Message message)
        {
            if (message == null || !message.IsRequest())
            {
                return null;
            }

            PeerAddress recipient = message.Recipient;

            // search for handler, 0 is ping
            // if we send with peerId = ZERO, then we take the first one we found
            if (recipient.PeerId.IsZero && message.Command == Rpc.Rpc.Commands.Ping.GetNr())
            {
                Number160 peerId = _peerBeanMaster.ServerPeerAddress.PeerId;
                return SearchHandler(peerId, peerId, Rpc.Rpc.Commands.Ping.GetNr());
            }
            else
            {
                // else we search for the handler that we are responsible for
                DispatchHandler handler = SearchHandler(recipient.PeerId, recipient.PeerId, message.Command);
                if (handler != null)
                {
                    return handler;
                }
                else
                {
                    // If we could not find a handler that we are responsible for, we
                    // are most likely a relay. Since we have no ID of the relay, we
                    // just take the first one.
                    var handlers = SearchHandler(Convert.ToInt32(message.Command));
                    foreach (var entry in handlers)
                    {
                        if (entry.Key.DomainKey.Equals(recipient.PeerId))
                        {
                            return entry.Value;
                        }
                    }
                    return null;
                }
            }
        }

        /// <summary>
        /// Looks for a registered handler according to the given parameters.
        /// </summary>
        /// <param name="recipientId">The ID of the recipient of the message.</param>
        /// <param name="onBehalfOf">The ID of the onBehalfOf peer.</param>
        /// <param name="command">The command of the message to be filtered for.</param>
        /// <returns>The handler for the provided parameters or null, if none has been found.</returns>
        public DispatchHandler SearchHandler(Number160 recipientId, Number160 onBehalfOf, int command)
        {
            IDictionary<int, DispatchHandler> commands = _ioHandlers[new Number320(recipientId, onBehalfOf)];

            if (commands != null && commands.ContainsKey(command))
            {
                return commands[command];
            }
            else
            {
                // not registered
                Logger.Debug("Handler not found for command {0}. Looking for the server with ID {1}.", command, recipientId);
                return null;
            }
        }

        private IEnumerable<KeyValuePair<Number320, DispatchHandler>> SearchHandler(int command)
        {
            IDictionary<Number320, DispatchHandler> result = new Dictionary<Number320, DispatchHandler>();
            foreach (var entry in _ioHandlers)
            {
                foreach (var entry2 in entry.Value)
                {
                    var handler = entry.Value[command];
                    if (handler != null && entry2.Key == command)
                    {
                        result.Add(entry.Key, handler);
                    }
                }
            }
            return result;
        }
    }
}
