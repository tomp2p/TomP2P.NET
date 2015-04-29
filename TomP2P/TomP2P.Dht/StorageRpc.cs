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
using TomP2P.Core.Utils;

namespace TomP2P.Dht
{
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


        }

        public override void HandleResponse(Core.Message.Message requestMessage, Core.Connection.PeerConnection peerConnection, bool sign, Core.Connection.IResponder responder)
        {
            throw new NotImplementedException();
        }
    }
}
