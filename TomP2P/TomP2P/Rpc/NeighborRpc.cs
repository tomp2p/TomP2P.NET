using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public const int NeighborSize = 30;
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
            if (requestMessage.KeyList.Count < 2)
            {
                throw new ArgumentException("At least location and domain keys are needed.");
            }
            if (!(requestMessage.Type == Message.Message.MessageType.Request1
                || requestMessage.Type == Message.Message.MessageType.Request2
                || requestMessage.Type == Message.Message.MessageType.Request3
                || requestMessage.Type == Message.Message.MessageType.Request4)
                && (requestMessage.Command == Rpc.Commands.Neighbor.GetNr()))
            {
                throw new ArgumentException("Message content is wrong for this handler.");
            }

            Number160 locationKey = requestMessage.Key(0);
            Number160 domainKey = requestMessage.Key(1);

            var neighbors = GetNeighbors(locationKey, NeighborSize);
            if (neighbors == null)
            {
                // return empty neighbor set
                var response = CreateResponseMessage(requestMessage, Message.Message.MessageType.NotFound);
                response.SetNeighborSet(new NeighborSet(-1, new Collection<PeerAddress>()));
                responder.Response(response);
                return;
            }

            // create response message and set neighbors
            var responseMessage = CreateResponseMessage(requestMessage, Message.Message.MessageType.Ok);

            Logger.Debug("Found the following neighbors: {0}.", neighbors);
            var neighborSet = new NeighborSet(NeighborLimit, neighbors);
            responseMessage.SetNeighborSet(neighborSet);

            Number160 contentKey = requestMessage.Key(2);
            var keyBloomFilter = requestMessage.BloomFilter(0);
            var contentBloomFilter = requestMessage.BloomFilter(1);
            var keyCollection = requestMessage.KeyCollection(0);

            // it is important to set an integer if a value is present
            bool isDigest = requestMessage.Type != Message.Message.MessageType.Request1;
            if (isDigest)
            {
                if (requestMessage.Type == Message.Message.MessageType.Request2)
                {
                    DigestInfo digestInfo;
                    if (PeerBean.DigestStorage == null)
                    {
                        // no storage to search
                        digestInfo = new DigestInfo();
                    }
                    else if (contentKey != null && locationKey != null && domainKey != null)
                    {
                        var locationAndDomainKey = new Number320(locationKey, domainKey);
                        var from = new Number640(locationAndDomainKey, contentKey, Number160.Zero);
                        var to = new Number640(locationAndDomainKey, contentKey, Number160.MaxValue);
                        digestInfo = PeerBean.DigestStorage.Digest(from, to, -1, true);
                    }
                    else if ((keyBloomFilter != null || contentBloomFilter != null) && locationKey != null && domainKey != null)
                    {
                        var locationAndDomainKey = new Number320(locationKey, domainKey);
                        digestInfo = PeerBean.DigestStorage.Digest(locationAndDomainKey, keyBloomFilter,
                                contentBloomFilter, -1, true, true);
                    }
                    else if (keyCollection != null && keyCollection.Keys.Count == 2)
                    {
                        var enumerator = keyCollection.Keys.GetEnumerator();
                        var from = enumerator.MoveNext() ? enumerator.Current : null; // TODO works correctly?
                        var to = enumerator.MoveNext() ? enumerator.Current : null;

                        digestInfo = PeerBean.DigestStorage.Digest(from, to, -1, true);
                    }
                    else if (locationKey != null && domainKey != null)
                    {
                        var locationAndDomainKey = new Number320(locationKey, domainKey);
                        var from = new Number640(locationAndDomainKey, Number160.Zero, Number160.Zero);
                        var to = new Number640(locationAndDomainKey, Number160.MaxValue, Number160.MaxValue);
                        digestInfo = PeerBean.DigestStorage.Digest(from, to, -1, true);
                    }
                    else
                    {
                        Logger.Warn("Did not search for anything.");
                        digestInfo = new DigestInfo();
                    }
                    responseMessage.SetIntValue(digestInfo.Size);
                    responseMessage.SetKey(digestInfo.KeyDigest);
                    responseMessage.SetKey(digestInfo.ContentDigest);
                }
                else if (requestMessage.Type == Message.Message.MessageType.Request3)
                {
                    DigestInfo digestInfo;
				    if (PeerBean.DigestTracker == null) {
					    // no tracker to search
					    digestInfo = new DigestInfo();
				    }
                    else
                    {
					    digestInfo = PeerBean.DigestTracker.Digest(locationKey, domainKey, contentKey);
					    if (digestInfo.Size == 0)
                        {
						    Logger.Debug("No entry found on peer {0}.", requestMessage.Recipient);
					    }
				    }
                    responseMessage.SetIntValue(digestInfo.Size);
                }
                else if (requestMessage.Type == Message.Message.MessageType.Request4)
                {
                    lock (PeerBean.PeerStatusListeners)
                    {
                        foreach (var listener in PeerBean.PeerStatusListeners)
                        {
                            listener.PeerFailed(requestMessage.Sender,
                                new PeerException(PeerException.AbortCauseEnum.Shutdown, "shutdown"));
                        }
                    }
                }
            }

            responder.Response(responseMessage);
        }

        // TODO in Java: explain why protected method here
        protected ICollection<PeerAddress> GetNeighbors(Number160 id, int atLeast)
        {
            // TODO adapt to newest TomP2P version
            return PeerBean.PeerMap.ClosePeers(id, atLeast);
        }
    }
}
