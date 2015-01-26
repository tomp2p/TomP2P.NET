using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.NET_Helper;
using TomP2P.Connection.Windows;
using TomP2P.Extensions;
using TomP2P.Futures;
using TomP2P.Message;
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
        /// <param name="isFireAndForget">True, if handler == null.</param>
        /// <param name="tcs">The TCS for the response message. (FutureResponse equivalent.)</param>
        /// <param name="message">The message to send.</param>
        /// <param name="channelCreator">The channel creator for the UDP channel.</param>
        /// <param name="idleUdpSeconds">The idle time of a message until fail.</param>
        /// <param name="broadcast">True, if the message is to be sent via layer 2 broadcast.</param>
        /// <returns>The response message or null, if it is fire-and-forget or a failure occurred.</returns>
        public Message.Message SendUdp(bool isFireAndForget, TaskCompletionSource<Message.Message> tcs, Message.Message message, ChannelCreator channelCreator, int idleUdpSeconds, bool broadcast)
        {
            // no need to continue if already finished
            if (tcs.Task.IsCompleted)
            {
                return tcs.Task.Result;
            }
            RemovePeerIfFailed(tcs, message);

            // TODO how to use timeouts?? -> use param idleUdpSeconds

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
                    const string msg = "Peer is relayed, but no relay is given.";
                    Logger.Error(msg);
                    tcs.SetException(new TaskFailedException(msg));
                    return null;
                }
            }

            // 4. check for invalid UDP connection to unreachable peers)
            if (message.Recipient.IsRelayed && message.Command != Rpc.Rpc.Commands.Neighbor.GetNr()
                && message.Command != Rpc.Rpc.Commands.Ping.GetNr())
            {
                string msg =
                    String.Format(
                        "Tried to send a UDP message to unreachable peers. Only TCP messages can be sent to unreachable peers: {0}.",
                        message);
                Logger.Warn(msg);
                tcs.SetException(new TaskFailedException(msg));
                return null;
            }

            // 5. create UDP channel
            //  - extract sender EP from message (in Java, this is done in TomP2POutbound)
            //  - check resource constraints
            var senderEp = ConnectionHelper.ExtractSenderEp(message);
            var receiverEp = ConnectionHelper.ExtractReceiverEp(message);
            Logger.Debug("Send UDP message {0}: Sender {1} --> Recipient {2}.", message, senderEp, receiverEp);

            MyUdpClient udpClient = channelCreator.CreateUdp(broadcast, senderEp);

            // check if channel could be created (due to resource constraints)
            if (udpClient == null)
            {
                const string msg = "Could not create a UDP socket. (Due to resource constraints.)";
                Logger.Warn(msg);
                tcs.SetException(new TaskFailedException(msg));
                return null;
                
                // TODO add reason for fail (e.g., from SocketException), e.g. move to ChannelCreator
                Logger.Debug("Channel creation failed.");
                // may have been closed by the other side
                // or it may have been canceled from this side
            }
            Logger.Debug("About to connect to {0} with channel {1}, ff = {2}.", message.Recipient, udpClient, isFireAndForget);

            // 3. client-side pipeline (sending)
            //  - encoder
            var outbound = new TomP2POutbound(false, ChannelClientConfiguration.SignatureFactory);
            var buffer = outbound.Write(message); // encode
            var bytes = ConnectionHelper.ExtractBytes(buffer);
            
            // 6. send/write message to the created channel
            //Task<int> sendTask = udpClient.SendAsync(bytes, bytes.Length, receiverEp);
            //await sendTask;

            try
            {
                udpClient.Send(bytes, bytes.Length, receiverEp);
            }
            catch (Exception ex)
            {
                // fail sending
                string msg = String.Format("Exception(s) when sending {0}: {1}.", message, ex);
                Logger.Error(msg);
                tcs.SetException(new TaskFailedException(msg));
                udpClient.NotifiedClose();
                return null;
            }

            // success for sending
            // await response, if not fire&forget
            if (isFireAndForget)
            {
                // close channel now
                Logger.Debug("Fire and forget message {0} sent. Close channel {1} now.", message, udpClient);
                udpClient.NotifiedClose();
                return null; // return FF response
            }
            else
            {
                // receive response message
                //Task<UdpReceiveResult> recvTask = udpClient.ReceiveAsync();
                //await recvTask;
                var remoteEp = new IPEndPoint(IPAddress.Any, 0); // TODO correct? or should MyUdpServer receive answer?

                byte[] recvBytes;
                try
                {
                    recvBytes = udpClient.Receive(ref remoteEp);
                }
                catch (Exception ex)
                {
                    // fail receiving
                    string msg = String.Format("One or more exceptions occurred when receiving: {0}.", ex);
                    Logger.Error(msg);
                    tcs.SetException(new TaskFailedException(msg));
                    udpClient.NotifiedClose();
                    return null;
                }

                // success for receiving
                // decode message
                var singlePacketUdp = new TomP2PSinglePacketUDP(ChannelClientConfiguration.SignatureFactory);
                var responseMessage = singlePacketUdp.Read(recvBytes, (IPEndPoint)udpClient.Client.LocalEndPoint, remoteEp); 

                // return response message
                //tcs.SetResult(responseMessage);
                udpClient.NotifiedClose();
                return responseMessage;
            }
        }

        /// <summary>
        /// Sends a message via TCP.
        /// </summary>
        /// <param name="isFireAndFroget">True, if handler == null.</param>
        /// <param name="tcs">The TCS for the response message. (FutureResponse equivalent.)</param>
        /// <param name="message">The message to send.</param>
        /// <param name="channelCreator">The channel creator for the TCP channel.</param>
        /// <param name="idleTcpSeconds">The idle time until message fail.</param>
        /// <param name="connectTimeoutMillis">The idle time for the connection setup.</param>
        /// <param name="peerConnection"></param>
        /// <returns></returns>
        public Message.Message SendTcp(bool isFireAndFroget, TaskCompletionSource<Message.Message> tcs,
            Message.Message message, ChannelCreator channelCreator, int idleTcpSeconds, int connectTimeoutMillis,
            PeerConnection peerConnection)
        {
            // no need to continue if already finished
            if (tcs.Task.IsCompleted)
            {
                return tcs.Task.Result;
            }
            RemovePeerIfFailed(tcs, message);

            // we need to set the neighbors if we use relays
            if (message.Sender.IsRelayed && message.Sender.PeerSocketAddresses.Count != 0)
            {
                message.SetPeerSocketAddresses(message.Sender.PeerSocketAddresses);
            }

            if (peerConnection != null && p)
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
    }
}
