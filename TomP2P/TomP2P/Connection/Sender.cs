using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
                    return null;
                }
            }

            // 4. check for invalid UDP connection to unreachable peers)
            if (message.Recipient.IsRelayed && message.Command != Rpc.Rpc.Commands.Neighbor.GetNr()
                && message.Command != Rpc.Rpc.Commands.Ping.GetNr())
            {
                Logger.Warn("Tried to send a UDP message to unreachable peers. Only TCP messages can be sent to unreachable peers: {0}.", message);
                // TODO set task to failed
                return null;
            }

            // 5. create UDP channel
            //  - extract sender EP from message (in Java, this is done in TomP2POutbound)
            //  - check resource constraints
            var senderEp = ConnectionHelper.ExtractSenderEp(message);
            var receiverEp = ConnectionHelper.ExtractReceiverEp(message);
            Logger.Debug("Send UDP message {0}: Sender {1} --> Recipient {2}.", message, senderEp, receiverEp);

            UdpClient udpClient = channelCreator.CreateUdp(broadcast, senderEp);

            // check if channel could be created due to resource constraints
            if (udpClient == null)
            {
                Logger.Warn("Could not create a {0} socket. (Due to resource constraints.)", message.IsUdp ? "UDP" : "TCP");
                // TODO set task to failed
                Logger.Debug("Channel creation failed.");
                // may have been closed by the other side
                // or it may have been canceled from this side
                // TODO add reason for fail (e.g., from SocketException)
                return null;
            }

            // TODO Java uses a DatagramPacket wrapper -> interoperability issue?
            // 3. client-side pipeline (sending)
            //  - encoder
            var outbound = new TomP2POutbound(false, ChannelClientConfiguration.SignatureFactory);
            var buffer = outbound.Write(message); // encode
            var bytes = ConnectionHelper.ExtractBytes(buffer);
            
            // 6. send/write message to the created channel
            Task<int> sendTask = udpClient.SendAsync(bytes, bytes.Length, receiverEp);

            // 7. await response message (if not fire&forget)
            // 9. handle possible errors during send (normal vs. fire&forget)
            //  - decoder
            if (isFireAndForget)
            {
                // close channel now
                Logger.Debug("Fire and forget message {0} sent. Close channel {1} now.", message, udpClient);
                udpClient.Close(); // TODO ok? what happens when during sending operation? (linger option?)
                
                // TODO report message
                return null; // TODO null for signaling fire&forget ok?
            }
            else
            {
                // not fire&forget -> await response
                await sendTask;
                if (sendTask.Exception != null)
                {
                    // fail sending
                    // TODO report failed
                    Logger.Warn("One or more exceptions occurred when sending {0}: {1}.", message, sendTask.Exception);
                    return null;
                }
                else
                {
                    // success for sending
                    // await response
                    Task<UdpReceiveResult> recvTask = udpClient.ReceiveAsync();
                    await recvTask;
                    if (recvTask.Exception != null)
                    {
                        // fail receiving
                        // TODO report failed
                        Logger.Warn("One or more exceptions occurred when receiving: {0}.", recvTask.Exception);
                        return null;
                    }
                    else
                    {
                        // success for receiving
                        // decode message
                        var singlePacketUdp = new TomP2PSinglePacketUDP(ChannelClientConfiguration.SignatureFactory);

                        IPEndPoint remoteEndPoint = recvTask.Result.RemoteEndPoint;
                        byte[] recvBytes = recvTask.Result.Buffer;

                        var responseMessage = singlePacketUdp.Read(recvBytes, receiverEp, senderEp); // TODO correct? or use remoteEp from returned dgram?
                        // return response message to the RequestHandler
                        return responseMessage;
                    }
                }
            }

            // 8. close channel/socket -> ChannelCreator -> SetupCloseListener
            udpClient.Close();
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
