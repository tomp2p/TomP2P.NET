using TomP2P.Core.Message;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Dht
{
    public class SendBuilder : DhtBuilder<SendBuilder>, ISendDirectBuilder
    {
        private static readonly TcsSend TcsSendShutdown = new TcsSend(null);

        public Buffer Buffer { get; private set; }
        public object Object { get; private set; }

        public bool IsCancelOnFinish { get; private set; }
        public bool IsStreaming { get; private set; }

        // static constructor
        static SendBuilder()
        {
            TcsSendShutdown.SetException(new TaskFailedException("Peer is shutting down."));
        }

        public SendBuilder(PeerDht peerDht, Number160 locationKey)
            : base(peerDht, locationKey)
        {
            SetSelf(this);
        }

        public TcsSend Start()
        {
            if (PeerDht.Peer.IsShutdown)
            {
                return TcsSendShutdown;
            }
            PreBuild();
            return PeerDht.Dht.Direct(this);
        }

        public SendBuilder SetBuffer(Buffer buffer)
        {
            Buffer = buffer;
            return this;
        }

        public SendBuilder SetObject(object obj)
        {
            Object = obj;
            return this;
        }

        public bool IsRaw
        {
            get { return Object == null; }
        }

        public SendBuilder SetIsCancelOnFinish()
        {
            return SetIsCancelOnFinish(true);
        }

        public SendBuilder SetIsCancelOnFinish(bool isCancelOnFinish)
        {
            IsCancelOnFinish = isCancelOnFinish;
            return this;
        }

        public SendBuilder SetIsStreaming()
        {
            return SetIsStreaming(true);
        }

        public SendBuilder SetIsStreaming(bool isStreaming)
        {
            IsStreaming = isStreaming;
            return this;
        }
    }
}
