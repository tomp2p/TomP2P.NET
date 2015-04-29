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
                dataMap = new DataMap(putBuilder.LocationKey, putBuilder.DomainKey, putBuilder.VersionKey, putBuilder.DataMapConvert);
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
                dataMap = new DataMap(putBuilder.LocationKey, putBuilder.DomainKey, putBuilder.VersionKey, putBuilder.DataMapConvert);
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
                dataMap = new DataMap(putBuilder.LocationKey, putBuilder.DomainKey, putBuilder.VersionKey, putBuilder.DataMapConvert);
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
                    throw new ArgumentException("Null not allowed as parameter.");
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
            
        }

        public override void HandleResponse(Message requestMessage, PeerConnection peerConnection, bool sign, IResponder responder)
        {
            throw new NotImplementedException();
        }
    }
}
