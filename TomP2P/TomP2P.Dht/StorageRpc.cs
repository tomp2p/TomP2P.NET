using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Core.Connection;
using TomP2P.Core.Message;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;
using TomP2P.Core.Utils;

namespace TomP2P.Dht
{
    // TODO the put methods have a lot in common -> extract methods (also in Java)
    public class StorageRpc : DispatchHandler
    {
        public static readonly SimpleBloomFilter<Number160> EmptyFilter = new SimpleBloomFilter<Number160>(0, 0);
        public static readonly SimpleBloomFilter<Number160> FullFilter = new SimpleBloomFilter<Number160>(8, 1);

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Random Rnd = new Random(); // TODO InteropRandom required?

        private readonly IBloomfilterFactory _bloomfilterFactory;
        private readonly StorageLayer _storageLayer;
        public IReplicationListener ReplicationListener { get; private set; }

        // static constructor
        static StorageRpc()
        {
            FullFilter.SetAll();
        }

        /// <summary>
        /// Registers the storage RPC for PUT, COMPARE PUT, GET, ADD and REMOVE.
        /// </summary>
        /// <param name="peerBean"></param>
        /// <param name="connectionBean"></param>
        /// <param name="storageLayer"></param>
        public StorageRpc(PeerBean peerBean, ConnectionBean connectionBean, StorageLayer storageLayer)
            : base(peerBean, connectionBean)
        {
            Register(
                Rpc.Commands.Put.GetNr(),
                Rpc.Commands.Get.GetNr(),
                Rpc.Commands.Add.GetNr(),
                Rpc.Commands.Remove.GetNr(),
                Rpc.Commands.Digest.GetNr(),
                Rpc.Commands.DigestBloomfilter.GetNr(),
                Rpc.Commands.DigestAllBloomfilter.GetNr(),
                Rpc.Commands.PutMeta.GetNr(),
                Rpc.Commands.DigestMetaValues.GetNr(),
                Rpc.Commands.PutConfirm.GetNr(),
                Rpc.Commands.GetLatest.GetNr(),
                Rpc.Commands.GetLatestWithDigest.GetNr(),
                Rpc.Commands.ReplicaPut.GetNr());
            _bloomfilterFactory = peerBean.BloomfilterFactory;
            _storageLayer = storageLayer;
        }

        public StorageRpc SetReplicationListener(IReplicationListener replicationListener)
        {
            ReplicationListener = replicationListener;
            return this;
        }

        /// <summary>
        /// Stores data on a remote peer. Overwrites data if the data already exists.
        /// </summary>
        /// <param name="remotePeer">The remote peer on which to store the data.</param>
        /// <param name="putBuilder">The builder to use for this operation.</param>
        /// <param name="channelCreator">The channel creator that will be used.</param>
        /// <returns>The future response message.</returns>
        public Task<Message> PutAsync(PeerAddress remotePeer, PutBuilder putBuilder, ChannelCreator channelCreator)
        {
            var type = putBuilder.IsProtectDomain ? Message.MessageType.Request2 : Message.MessageType.Request1;
            return PutAsync(remotePeer, putBuilder, type, Rpc.Commands.Put, channelCreator);
        }

        /// <summary>
        /// Stores data on a remote peer. Only stores data if the data does not already exist.
        /// </summary>
        /// <param name="remotePeer">The remote peer on which to store the data.</param>
        /// <param name="putBuilder">The builder to use for this operation.</param>
        /// <param name="channelCreator">The channel creator that will be used.</param>
        /// <returns>The future response message.</returns>
        public Task<Message> PutIfAbsent(PeerAddress remotePeer, PutBuilder putBuilder, ChannelCreator channelCreator)
        {
            var type = putBuilder.IsProtectDomain ? Message.MessageType.Request4 : Message.MessageType.Request3;
            return PutAsync(remotePeer, putBuilder, type, Rpc.Commands.Put, channelCreator);
        }

        public Task<Message> PutReplica(PeerAddress remotePeer, PutBuilder putBuilder, ChannelCreator channelCreator)
        {
            return PutAsync(remotePeer, putBuilder, Message.MessageType.Request1, Rpc.Commands.ReplicaPut,
                channelCreator);
        }

        private Task<Message> PutAsync(PeerAddress remotePeer, PutBuilder putBuilder, Message.MessageType type,
            Rpc.Commands command, ChannelCreator channelCreator)
        {
            Utils.NullCheck(remotePeer);

            DataMap dataMap;
            if (putBuilder.DataMap != null)
            {
                dataMap = new DataMap(putBuilder.DataMap);
            }
            else
            {
                dataMap = new DataMap(putBuilder.LocationKey, putBuilder.DomainKey, putBuilder.VersionKey,
                    putBuilder.DataMapConvert);
            }

            var message = CreateRequestMessage(remotePeer, command.GetNr(), type);

            if (putBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(putBuilder.KeyPair);
            }

            message.SetDataMap(dataMap);

            var tcsResponse = new TaskCompletionSource<Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, putBuilder);
            if (!putBuilder.IsForceUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            return requestHandler.SendUdpAsync(channelCreator);
        }

        public Task<Message> PutMetaAsync(PeerAddress remotePeer, PutBuilder putBuilder, ChannelCreator channelCreator)
        {
            Utils.NullCheck(remotePeer);

            DataMap dataMap;
            if (putBuilder.DataMap != null)
            {
                dataMap = new DataMap(putBuilder.DataMap);
            }
            else
            {
                dataMap = new DataMap(putBuilder.LocationKey, putBuilder.DomainKey, putBuilder.VersionKey,
                    putBuilder.DataMapConvert);
            }

            var type = putBuilder.ChangePublicKey != null ? Message.MessageType.Request2 : Message.MessageType.Request1;

            var message = CreateRequestMessage(remotePeer, Rpc.Commands.PutMeta.GetNr(), type);

            if (putBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(putBuilder.KeyPair);
            }
            else if (type == Message.MessageType.Request2)
            {
                throw new MemberAccessException("Can only change public key if message is signed.");
            }

            if (putBuilder.ChangePublicKey != null)
            {
                message.SetKey(putBuilder.LocationKey);
                message.SetKey(putBuilder.DomainKey);
                message.SetPublicKey(putBuilder.ChangePublicKey);
            }
            else
            {
                message.SetDataMap(dataMap);
            }

            var tcsResponse = new TaskCompletionSource<Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, putBuilder);

            if (!putBuilder.IsForceUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            return requestHandler.SendUdpAsync(channelCreator);
        }

        public Task<Message> PutConfirmAsync(PeerAddress remotePeer, PutBuilder putBuilder,
            ChannelCreator channelCreator)
        {
            Utils.NullCheck(remotePeer);

            DataMap dataMap;
            if (putBuilder.DataMap != null)
            {
                dataMap = new DataMap(putBuilder.DataMap);
            }
            else
            {
                dataMap = new DataMap(putBuilder.LocationKey, putBuilder.DomainKey, putBuilder.VersionKey,
                    putBuilder.DataMapConvert);
            }

            var message = CreateRequestMessage(remotePeer, Rpc.Commands.PutConfirm.GetNr(), Message.MessageType.Request1);

            if (putBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(putBuilder.KeyPair);
            }

            message.SetDataMap(dataMap);

            var tcsResponse = new TaskCompletionSource<Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, putBuilder);

            if (!putBuilder.IsForceUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            return requestHandler.SendUdpAsync(channelCreator);
        }

        /// <summary>
        /// Adds data on a remote peer. The main difference to PUT is that it will convert the data collection to a map.
        /// The key for the map will be the SHA-1 hash of the data.
        /// </summary>
        /// <param name="remotePeer">The remote peer on which to store the data.</param>
        /// <param name="addBuilder">The builder to use for this operation.</param>
        /// <param name="channelCreator">The channel creator that will be used.</param>
        /// <returns>The future response message.</returns>
        public Task<Message> AddAsync(PeerAddress remotePeer, AddBuilder addBuilder, ChannelCreator channelCreator)
        {
            Utils.NullCheck(remotePeer, addBuilder.LocationKey, addBuilder.DomainKey);

            Message.MessageType type;
            if (addBuilder.IsProtectDomain)
            {
                type = addBuilder.IsList ? Message.MessageType.Request4 : Message.MessageType.Request2;
            }
            else
            {
                type = addBuilder.IsList ? Message.MessageType.Request3 : Message.MessageType.Request1;
            }

            // convert the data
            var dataMap = new SortedDictionary<Number160, Data>();
            if (addBuilder.DataSet != null)
            {
                foreach (var data in addBuilder.DataSet)
                {
                    if (addBuilder.IsList)
                    {
                        Number160 hash;
                        do
                        {
                            hash = new Number160(addBuilder.Random);
                        } while (dataMap.ContainsKey(hash));
                        dataMap.Add(hash, data);
                    }
                    else
                    {
                        dataMap.Add(data.Hash, data);
                    }
                }
            }

            var message = CreateRequestMessage(remotePeer, Rpc.Commands.Add.GetNr(), type);

            if (addBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(addBuilder.KeyPair);
            }

            message.SetDataMap(new DataMap(addBuilder.LocationKey, addBuilder.DomainKey, addBuilder.VersionKey, dataMap));

            var tcsResponse = new TaskCompletionSource<Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, addBuilder);

            if (!addBuilder.IsForceUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            return requestHandler.SendUdpAsync(channelCreator);
        }

        public Task<Message> DigestAsync(PeerAddress remotePeer, DigestBuilder digestBuilder,
            ChannelCreator channelCreator)
        {
            sbyte command;
            if (digestBuilder.IsReturnBloomFilter)
            {
                command = Rpc.Commands.DigestBloomfilter.GetNr();
            }
            else if (digestBuilder.IsReturnMetaValues)
            {
                command = Rpc.Commands.DigestMetaValues.GetNr();
            }
            else if (digestBuilder.IsReturnAllBloomFilter)
            {
                command = Rpc.Commands.DigestAllBloomfilter.GetNr();
            }
            else
            {
                command = Rpc.Commands.Digest.GetNr();
            }

            Message.MessageType type;
            if (digestBuilder.IsAscending && digestBuilder.IsBloomFilterAnd)
            {
                type = Message.MessageType.Request1;
            }
            else if (!digestBuilder.IsAscending && digestBuilder.IsBloomFilterAnd)
            {
                type = Message.MessageType.Request2;
            }
            else if (!digestBuilder.IsAscending && !digestBuilder.IsBloomFilterAnd)
            {
                type = Message.MessageType.Request3;
            }
            else
            {
                type = Message.MessageType.Request4;
            }

            var message = CreateRequestMessage(remotePeer, command, type);
            if (digestBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(digestBuilder.KeyPair);
            }
            if (digestBuilder.IsRange)
            {
                var keys = new List<Number640>(2);
                keys.Add(digestBuilder.From);
                keys.Add(digestBuilder.To);
                message.SetIntValue(digestBuilder.ReturnNr);
                message.SetKeyCollection(new KeyCollection(keys));
            }
            else if (digestBuilder.Keys == null)
            {
                if (digestBuilder.LocationKey == null || digestBuilder.DomainKey == null)
                {
                    throw new ArgumentException("Null not allowed in location or domain.");
                }
                message.SetKey(digestBuilder.LocationKey);
                message.SetKey(digestBuilder.DomainKey);

                if (digestBuilder.ContentKeys != null)
                {
                    message.SetKeyCollection(new KeyCollection(digestBuilder.LocationKey, digestBuilder.DomainKey,
                        digestBuilder.VersionKey, digestBuilder.ContentKeys));
                }
                else
                {
                    message.SetIntValue(digestBuilder.ReturnNr);
                    if (digestBuilder.KeyBloomFilter != null || digestBuilder.ContentBloomFilter != null)
                    {
                        if (digestBuilder.KeyBloomFilter != null)
                        {
                            message.SetBloomFilter(digestBuilder.KeyBloomFilter);
                        }
                        if (digestBuilder.ContentBloomFilter != null)
                        {
                            message.SetBloomFilter(digestBuilder.ContentBloomFilter);
                        }
                    }
                }
            }
            else
            {
                message.SetKeyCollection(new KeyCollection(digestBuilder.Keys));
            }

            var tcsResponse = new TaskCompletionSource<Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, digestBuilder);

            if (!digestBuilder.IsForceUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            return requestHandler.SendUdpAsync(channelCreator);
        }

        public Task<Message> GetAsync(PeerAddress remotePeer, GetBuilder getBuilder, ChannelCreator channelCreator)
        {
            Message.MessageType type;
            if (getBuilder.IsAscending && getBuilder.IsBloomFilterAnd)
            {
                type = Message.MessageType.Request1;
            }
            else if (!getBuilder.IsAscending && getBuilder.IsBloomFilterAnd)
            {
                type = Message.MessageType.Request2;
            }
            else if (getBuilder.IsAscending && !getBuilder.IsBloomFilterAnd)
            {
                type = Message.MessageType.Request3;
            }
            else
            {
                type = Message.MessageType.Request4;
            }

            var message = CreateRequestMessage(remotePeer, Rpc.Commands.Get.GetNr(), type);
            if (getBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(getBuilder.KeyPair);
            }
            if (getBuilder.IsRange)
            {
                var keys = new List<Number640>(2);
                keys.Add(getBuilder.From);
                keys.Add(getBuilder.To);
                message.SetIntValue(getBuilder.ReturnNr);
                message.SetKeyCollection(new KeyCollection(keys));
            }
            else if (getBuilder.Keys == null)
            {
                if (getBuilder.LocationKey == null || getBuilder.DomainKey == null)
                {
                    throw new ArgumentException("Null not allowed in location or domain.");
                }
                message.SetKey(getBuilder.LocationKey);
                message.SetKey(getBuilder.DomainKey);

                if (getBuilder.ContentKeys != null)
                {
                    message.SetKeyCollection(new KeyCollection(getBuilder.LocationKey, getBuilder.DomainKey,
                        getBuilder.VersionKey, getBuilder.ContentKeys));
                }
                else
                {
                    message.SetIntValue(getBuilder.ReturnNr);

                    if (getBuilder.ContentKeyBloomFilter != null)
                    {
                        message.SetBloomFilter(getBuilder.ContentKeyBloomFilter);
                    }
                    else
                    {
                        if (getBuilder.IsBloomFilterAnd)
                        {
                            message.SetBloomFilter(FullFilter);
                        }
                        else
                        {
                            message.SetBloomFilter(EmptyFilter);
                        }
                    }

                    if (getBuilder.VersionKeyBloomFilter != null)
                    {
                        message.SetBloomFilter(getBuilder.VersionKeyBloomFilter);
                    }
                    else
                    {
                        if (getBuilder.IsBloomFilterAnd)
                        {
                            message.SetBloomFilter(FullFilter);
                        }
                        else
                        {
                            message.SetBloomFilter(EmptyFilter);
                        }
                    }

                    if (getBuilder.ContentBloomFilter != null)
                    {
                        message.SetBloomFilter(getBuilder.ContentBloomFilter);
                    }
                    else
                    {
                        if (getBuilder.IsBloomFilterAnd)
                        {
                            message.SetBloomFilter(FullFilter);
                        }
                        else
                        {
                            message.SetBloomFilter(EmptyFilter);
                        }
                    }
                }
            }
            else
            {
                message.SetKeyCollection(new KeyCollection(getBuilder.Keys));
            }

            var tcsResponse = new TaskCompletionSource<Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, getBuilder);
            if (!getBuilder.IsForceUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            return requestHandler.SendUdpAsync(channelCreator);
        }

        public Task<Message> GetLatestAsync(PeerAddress remotePeer, GetBuilder getBuilder, ChannelCreator channelCreator,
            Rpc.Commands command)
        {
            var message = CreateRequestMessage(remotePeer, command.GetNr(), Message.MessageType.Request1);
            if (getBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(getBuilder.KeyPair);
            }
            message.SetKey(getBuilder.LocationKey);
            message.SetKey(getBuilder.DomainKey);
            message.SetKey(getBuilder.ContentKey);

            var tcsResponse = new TaskCompletionSource<Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, getBuilder);
            if (!getBuilder.IsForceUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            return requestHandler.SendUdpAsync(channelCreator);
        }

        /// <summary>
        /// Removes data from a peer.
        /// </summary>
        /// <param name="remotePeer">The remote peer on which to store the data.</param>
        /// <param name="removeBuilder">The builder to use for this operation.</param>
        /// <param name="channelCreator">The channel creator that will be used.</param>
        /// <returns>The future response message.</returns>
        public Task<Message> RemoveAsync(PeerAddress remotePeer, RemoveBuilder removeBuilder,
            ChannelCreator channelCreator)
        {
            var message = CreateRequestMessage(remotePeer, Rpc.Commands.Remove.GetNr(),
                removeBuilder.IsReturnResults ? Message.MessageType.Request2 : Message.MessageType.Request1);
            if (removeBuilder.IsSign)
            {
                message.SetPublicKeyAndSign(removeBuilder.KeyPair);
            }
            if (removeBuilder.IsRange)
            {
                var keys = new List<Number640>(2);
                keys.Add(removeBuilder.From);
                keys.Add(removeBuilder.To);
                message.SetIntValue(0); // marker
                message.SetKeyCollection(new KeyCollection(keys));
            }
            else if (removeBuilder.Keys == null)
            {
                if (removeBuilder.LocationKey == null || removeBuilder.DomainKey == null)
                {
                    throw new ArgumentException("Null not allowed in location or domain.");
                }
                message.SetKey(removeBuilder.LocationKey);
                message.SetKey(removeBuilder.DomainKey);

                if (removeBuilder.ContentKeys != null)
                {
                    message.SetKeyCollection(new KeyCollection(removeBuilder.LocationKey, removeBuilder.DomainKey,
                        removeBuilder.VersionKey, removeBuilder.ContentKeys));
                }
            }
            else
            {
                message.SetKeyCollection(new KeyCollection(removeBuilder.Keys));
            }

            var tcsResponse = new TaskCompletionSource<Message>(message);
            var requestHandler = new RequestHandler(tcsResponse, PeerBean, ConnectionBean, removeBuilder);
            if (!removeBuilder.IsForceUdp)
            {
                return requestHandler.SendTcpAsync(channelCreator);
            }
            return requestHandler.SendUdpAsync(channelCreator);
        }

        public override void HandleResponse(Message requestMessage, PeerConnection peerConnection, bool sign,
            IResponder responder)
        {
            var responseMessage = CreateResponseMessage(requestMessage, Message.MessageType.Ok);

            if (requestMessage.Command == Rpc.Commands.Add.GetNr())
            {
                HandleAdd(requestMessage, responseMessage, IsDomainProtected(requestMessage));
            }
            else if (requestMessage.Command == Rpc.Commands.Put.GetNr()
                     || requestMessage.Command == Rpc.Commands.ReplicaPut.GetNr())
            {
                HandlePut(requestMessage, responseMessage, IsStoreIfAbsent(requestMessage),
                    IsDomainProtected(requestMessage), IsReplicaPut(requestMessage));
            }
            else if (requestMessage.Command == Rpc.Commands.PutConfirm.GetNr())
            {
                HandlePutConfirm(requestMessage, responseMessage);
            }
            else if (requestMessage.Command == Rpc.Commands.Get.GetNr())
            {
                HandleGet(requestMessage, responseMessage);
            }
            else if (requestMessage.Command == Rpc.Commands.GetLatest.GetNr())
            {
                HandleGetLatest(requestMessage, responseMessage, false);
            }
            else if (requestMessage.Command == Rpc.Commands.GetLatestWithDigest.GetNr())
            {
                HandleGetLatest(requestMessage, responseMessage, true);
            }
            else if (requestMessage.Command == Rpc.Commands.Digest.GetNr()
                     || requestMessage.Command == Rpc.Commands.DigestBloomfilter.GetNr()
                     || requestMessage.Command == Rpc.Commands.DigestMetaValues.GetNr()
                     || requestMessage.Command == Rpc.Commands.DigestAllBloomfilter.GetNr())
            {
                HandleDigest(requestMessage, responseMessage);
            }
            else if (requestMessage.Command == Rpc.Commands.Remove.GetNr())
            {
                HandleRemove(requestMessage, responseMessage,
                    requestMessage.Type == Message.MessageType.Request2);
            }
            else if (requestMessage.Command == Rpc.Commands.PutMeta.GetNr())
            {
                HandlePutMeta(requestMessage, responseMessage,
                    requestMessage.Type == Message.MessageType.Request2);
            }
            else
            {
                throw new ArgumentException($"Message content is wrong {requestMessage.Command}.");
            }
            if (sign)
            {
                responseMessage.SetPublicKeyAndSign(PeerBean.KeyPair);
            }
            Logger.Debug("Response for storage request: {0}.", responseMessage);
            responder.Response(responseMessage);
        }

        private static bool IsReplicaPut(Message message)
        {
            return message.Command == Rpc.Commands.ReplicaPut.GetNr();
        }

        private static bool IsDomainProtected(Message message)
        {
		    return message.PublicKey(0) != null && (message.Type == Message.MessageType.Request2 || message.Type == Message.MessageType.Request4);
	    }

        private static bool IsStoreIfAbsent(Message message)
        {
		    return message.Type == Message.MessageType.Request3 || message.Type == Message.MessageType.Request4;
	    }

        private static bool IsList(Message message)
        {
		    return message.Type == Message.MessageType.Request3 || message.Type == Message.MessageType.Request4;
	    }

        private static bool IsAscending(Message message)
        {
		    return message.Type == Message.MessageType.Request1 || message.Type == Message.MessageType.Request3;
	    }

        private static bool IsBloomFilterAnd(Message message)
        {
		    return message.Type == Message.MessageType.Request1 || message.Type == Message.MessageType.Request2;
	    }

        private void HandlePutMeta(Message message, Message responseMessage, bool isDomain)
        {
            
        }
    }
}
