using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty.Transport;
using TomP2P.Futures;
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
        /// <param name="handler">The handler to deal with a response message.</param>
        /// <param name="futureResponse">The future to set the response.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="channelCreator">The channel creator for the UDP channel.</param>
        /// <param name="idleUdpSeconds">The idle time of a message until fail.</param>
        /// <param name="broadcast">True, if the message is to be sent via layer 2 broadcast.</param>
        public void SendUdp(Inbox handler, FutureResponse futureResponse, Message.Message message,
            ChannelCreator channelCreator, int idleUdpSeconds, bool broadcast)
        {
            // TODO check if everything ok

            // no need to continue if already finished
            if (futureResponse.IsCompleted)
            {
                return;
            }
            //RemovePeerIfFailed(futureResponse, message);

            if (message.Sender.IsRelayed)
            {
                message.SetPeerSocketAddresses(message.Sender.PeerSocketAddresses);
            }

            bool isFireAndForget = handler == null;
            // TODO some handler configurations, probably not needed in .NET

            if (message.Recipient.IsRelayed && message.Command != Rpc.Rpc.Commands.Neighbor.GetNr()
                && message.Command != Rpc.Rpc.Commands.Ping.GetNr())
            {
                Logger.Warn("Tried to send a UDP message to unreachable peers. Only TCP messages can be sent to unreachable peers: {0}.", message);
                // TODO set task to failed...
            }
            else
            {
                if (message.Recipient.IsRelayed)
                {
                    IList<PeerSocketAddress> psa = new List<PeerSocketAddress>(message.Recipient.PeerSocketAddresses);
                    Logger.Debug("Send neighbor request to random relay peer {0}.", psa);
                    if (psa.Count > 0)
                    {
                        PeerSocketAddress address = psa[_random.NextInt(psa.Count)];
                        message.SetRecipientRelay(
                            message.Recipient.ChangePeerSocketAddress(address).ChangeIsRelayed(true));
                        channelCreator.CreateUdp(broadcast, handlers, futureResponse);
                    }
                    else
                    {
                        // set task to failed...
                        return;
                    }
                }
                else
                {
                    channelCreator.CreateUdp(broadcast, handlers, futureResponse);
                }
                //AfterConnect();
            }
        }
    }
}
