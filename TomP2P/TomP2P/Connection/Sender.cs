using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.NET_Helper;
using TomP2P.Connection.Windows;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;
using TomP2P.Futures;
using TomP2P.Message;
using TomP2P.Peers;
using TomP2P.Rpc;
using Encoder = TomP2P.Message.Encoder;

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
        /// <param name="message">The message to send.</param>
        /// <param name="channelCreator">The channel creator for the UDP channel.</param>
        /// <param name="idleUdpSeconds">The idle time of a message until fail.</param>
        /// <param name="broadcast">True, if the message is to be sent via layer 2 broadcast.</param>
        public async Task<Message.Message> SendUdpAsync(bool isFireAndForget, Message.Message message, ChannelCreator channelCreator, int idleUdpSeconds, bool broadcast)
        {
            // TODO check for sync completion
            // TODO RemovePeerIfFailed(futureResponse, message);

            // 2. fire & forget options
            
            // 1. relay options
            if (message.Sender.IsRelayed)
            {
                message.SetPeerSocketAddresses(message.Sender.PeerSocketAddresses);

                // TODO ok to do it here?
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
                    Logger.Error("Peer is relayed, but no relay given.");
                    // TODO set task to failed
                    return;
                }
            }

            // 4. check for invalid UDP connection to unreachable peers)
            if (message.Recipient.IsRelayed && message.Command != Rpc.Rpc.Commands.Neighbor.GetNr()
                && message.Command != Rpc.Rpc.Commands.Ping.GetNr())
            {
                Logger.Warn("Tried to send a UDP message to unreachable peers. Only TCP messages can be sent to unreachable peers: {0}.", message);
                // TODO set task to failed
                return;
            }

            // 5. create UDP channel
            //  - extract sender EP from message (in Java, this is done in TomP2POutbound)
            //  - check resource constraints
            var senderEp = ConnectionHelper.ExtractSenderEp(message);
            var receiverEp = ConnectionHelper.ExtractReceiverEp(message);
            Logger.Debug("Send UDP message {0}: Sender {1} --> Recipient {2}.", message, senderEp, receiverEp);

            UdpClientSocket udpClientSocket = channelCreator.CreateUdp(broadcast, senderEp);

            // check if channel could be created due to resource constraints
            if (udpClientSocket == null)
            {
                Logger.Warn("Could not create a {0} socket. (Due to resource constraints.)", message.IsUdp ? "UDP" : "TCP");
                // TODO set task to failed
                Logger.Debug("Channel creation failed.");
                // may have been closed by the other side
                // or it may have been canceled from this side
                // TODO add reason for fail (e.g., from SocketException)
                return;
            }

            // TODO Java uses a DatagramPacket wrapper -> interoperability issue?
            // 3. client-side pipeline (sending)
            //  - encoder
            var outbound = new TomP2POutbound(false, ChannelClientConfiguration.SignatureFactory);
            var buffer = outbound.Write(message); // encode
            var messageBytes = ConnectionHelper.ExtractBytes(buffer);
            
            // 6. send/write message to the created channel
            Task<int> task = udpClientSocket.SendAsync(messageBytes, receiverEp);

            // 7. await response message (if not fire&forget)
            // 9. handle possible errors during send (normal vs. fire&forget)
            //  - decoder
            if (isFireAndForget)
            {
                // close channel now
                Logger.Debug("Fire and forget message {0} sent. Close channel {1} now.", message, udpClientSocket);
                udpClientSocket.Close(); // TODO ok? what happens when during sending operation? (linger option?)
                
                // TODO report message
                return null; // TODO null for signaling fire&forget ok?
            }
            else
            {
                // not fire&forget -> await response
                await task;
                if (task.Exception != null)
                {
                    // fail
                    // TODO report failed
                    Logger.Warn("One or more exceptions occurred when sending {0}: {1}.", message, task.Exception);
                }
                else
                {
                    // success
                    // decode message

                    // return response message to the RequestHandler
                }
            }

            // 8. close channel/socket -> ChannelCreator -> SetupCloseListener
            udpClientSocket.Close();
        }

        /// <summary>
        /// After connecting, we check if the connect was successful.
        /// </summary>
        public async Task AfterConnect(Message.Message message, UdpClientSocket udpSocket, bool fireAndForget)
        {
            // this is actually the "callback"/successor of the SendX methods
            // we send the message here...

            // check if channel could be created due to resource constrains
            if (udpSocket == null)
            {
                Logger.Warn("Could not create a {} socket. (Due to resource constraints.)", message.IsUdp ? "UDP" : "TCP");
                // TODO set failed
                return;
            }
            Logger.Debug("About to connect to {0} with channel {1}, ff={2}.", message.Recipient, udpSocket, fireAndForget);

            // TODO cancellation token
            // in Java, channel creation is awaited -> on completion listener starts sending
            // in .NET, channel already exists -> we send right away
            
            // TODO how to send message? what EndPoint?
            // TODO remove
            Task writeFuture = udpSocket.Write(message, ChannelClientConfiguration);
            await AfterSendAsync(writeFuture, udpSocket, fireAndForget);
            // TODO report of possible channel creation exceptions
        }

        /// <summary>
        /// After sending, we check if the write was successful or if it was a fire and forget.
        /// </summary>
        /// <param name="writeFuture">The task of the write operation. Can be UDP or TCP.</param>
        /// <param name="fireAndForget">True, if we don't expect a message.</param>
        public async Task AfterSendAsync(Task writeFuture, UdpClientSocket udpSocket, bool fireAndForget)
        {
            // in Java, the async send operation is attached a listener
            // in .NET, we just await the async operation
            await writeFuture;
            if (writeFuture.Status != TaskStatus.RanToCompletion) // TODO correct status used?
            {
                ReportFailed(udpSocket);
                // TODO logging
            }
            if (fireAndForget)
            {
                // TODO logging
                ReportMessage(udpSocket);
            }
        }

        /// <summary>
        /// Report a failure after the channel was closed.
        /// </summary>
        /// <param name="udpSocket"></param>
        private void ReportFailed(UdpClientSocket udpSocket)
        {
            udpSocket.Close();
            // TODO responseNow();
        }

        /// <summary>
        /// Report a successful response after the channel was closed.
        /// </summary>
        /// <param name="udpSocket"></param>
        private void ReportMessage(UdpClientSocket udpSocket)
        {
            udpSocket.Close();
            // TODO responseNow();
        }
    }
}
