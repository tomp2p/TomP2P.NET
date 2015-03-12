using System.Collections.Generic;
using TomP2P.Core.Connection;
using TomP2P.Core.Message;
using TomP2P.Core.Peers;
using TomP2P.Core.Storage;

namespace TomP2P.Core.P2P.Builder
{
    public class BroadcastBuilder : DefaultConnectionConfiguration
    {
        private readonly Peer _peer;
        public Number160 MessageKey { get; private set; }
        public IDictionary<Number640, Data> DataMap { get; private set; }
        public bool IsUdp { get; private set; }
        private bool _udpManuallySet = false;
        public int HopCounter { get; private set; }

        public BroadcastBuilder(Peer peer, Number160 messageKey)
        {
            _peer = peer;
            MessageKey = messageKey;
        }

        public void Start()
        {
            var message = new Message.Message();

            if (!_udpManuallySet)
            {
                // not set, decide based on the data
                if (DataMap == null)
                {
                    SetIsUdp(true);
                }
                else
                {
                    SetIsUdp(false);
                    message.SetDataMap(new DataMap(DataMap));
                }
            }

            message.SetKey(MessageKey);
            message.SetIntValue(0);
            message.SetIsUdp(IsUdp);

            _peer.BroadcastRpc.BroadcastHandler.Receive(message);
        }

        public BroadcastBuilder SetDataMap(IDictionary<Number640, Data> dataMap)
        {
            DataMap = dataMap;
            return this;
        }

        public BroadcastBuilder SetIsUdp(bool isUdp)
        {
            IsUdp = isUdp;
            _udpManuallySet = true;
            return this;
        }

        public BroadcastBuilder SetHopCounter(int hopCounter)
        {
            HopCounter = hopCounter;
            return this;
        }

        public PeerAddress RemotePeer
        {
            get { return _peer.PeerAddress; }
        }
    }
}
