using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Message;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    /// <summary>
    /// Handles the neighbor requests and replies.
    /// </summary>
    public class NeighborRpc : DispatchHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public const int NeihborSize = 30;
        public const int NeighborLimit = 1000;

        public NeighborRpc(PeerBean peerBean, ConnectionBean connectionBean)
            : this(peerBean, connectionBean, true)
        { }

        /// <summary>
        /// Setup the RPC and register for incoming messages.
        /// </summary>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        /// <param name="register">Whether incoming messages should be registered.</param>
        public NeighborRpc(PeerBean peerBean, ConnectionBean connectionBean, bool register)
            : base(peerBean, connectionBean)
        {
            if (register)
            {
                Register(Rpc.Commands.Neighbor.GetNr());
            }
        }

        /// <summary>
        /// Requests close neighbors from the remote peer. The remote peer may indicate if the
        /// data is present on that peer. This is an RPC.
        /// </summary>
        /// <param name="remotePeer">The remote peer t send this request to.</param>
        /// <param name="searchValues">The values to search for in the storage.</param>
        /// <param name="type">The type of the neighbor request:
        /// - Request1 for Neighbors means check for Put (no digest) for tracker and storage.
        /// - Request2 for Neighbors means check for Get (with digest) for storage.
        /// - Request3 for Neighbors means check for Get (with digest) for tracker.
        /// - Request4 for Neighbors means check for Put (with digest) for task.</param>
        /// <param name="channelCreator">The channel creator that creates connections.</param>
        /// <param name="configuration">The client-side connection configuration.</param>
        /// <returns>The future response message.</returns>
        public Task<Message.Message> CloseNeighbors(PeerAddress remotePeer, SearchValues searchValues, Message.Message.MessageType type,
            ChannelCreator channelCreator, IConnectionConfiguration configuration)
        {
            var tcsResponse = CloseNeighborsTcs(remotePeer, searchValues, type, channelCreator, configuration);
            return tcsResponse.Task;
        }

        /// <summary>
        /// .NET-specific: Used for DistributedRouting only.
        /// </summary>
        internal TaskCompletionSource<Message.Message> CloseNeighborsTcs(PeerAddress remotePeer, SearchValues searchValues, Message.Message.MessageType type,
            ChannelCreator channelCreator, IConnectionConfiguration configuration)
        {
            var message = CreateRequestMessage(remotePeer, Rpc.Commands.Neighbor.GetNr(), type);
            if (!message.IsRequest())
            {
                throw new ArgumentException("The type must be a request.");
            }
            message.SetKey(searchValues.LocationKey);
            message.SetKey(searchValues.DomainKey ?? Number160.Zero);

            if (searchValues.From != null && searchValues.To != null)
            {
                ICollection<Number640> collection = new List<Number640>();
                collection.Add(searchValues.From);
                collection.Add(searchValues.To);
                var keyCollection = new KeyCollection(collection);
                message.SetKeyCollection(keyCollection);
            }
            else
            {
                if (searchValues.ContentKey != null)
                {
                    message.SetKey(searchValues.ContentKey);
                }
                if (searchValues.KeyBloomFilter != null)
                {
                    message.SetBloomFilter(searchValues.KeyBloomFilter);
                }
                if (searchValues.ContentBloomFilter != null)
                {
                    message.SetBloomFilter(searchValues.ContentBloomFilter);
                }
            }
            return Send(message, configuration, channelCreator);
        }

        private TaskCompletionSource<Message.Message> Send(Message.Message message, IConnectionConfiguration configuration,
            ChannelCreator channelCreator)
        {
            var tcsResponse = new TaskCompletionSource<Message.Message>(message);
            tcsResponse.Task.ContinueWith(taskResponse =>
            {
                if (!taskResponse.IsFaulted)
                {
                    var response = taskResponse.Result;
                    if (response != null)
                    {
                        var neighborSet = response.NeighborsSet(0);
                        if (neighborSet != null)
                        {
                            foreach (var neighbor in neighborSet.Neighbors)
                            {
                                lock (PeerBean.PeerStatusListeners)
                                {
                                    foreach (var listener in PeerBean.PeerStatusListeners)
                                    {
                                        listener.PeerFound(neighbor, response.Sender, null);
                                    }
                                }
                            }
                        }
                    }
                }
            });

            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, configuration);
            
            if (!configuration.IsForceTcp)
            {
                requestHandler.SendUdpAsync(channelCreator);
            }
            else
            {
                requestHandler.SendTcpAsync(channelCreator);
            }
            // .NET-specific: Return TCS instead of Task. It's actually the same TCS that is provided with
            // the RequestHandler c'tor
            return tcsResponse;
        }

        public override void HandleResponse(Message.Message requestMessage, PeerConnection peerConnection, bool sign, IResponder responder)
        {
            throw new NotImplementedException();
        }
    }
}
