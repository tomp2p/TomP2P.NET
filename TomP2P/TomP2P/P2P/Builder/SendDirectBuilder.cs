using System;
using TomP2P.Connection;
using TomP2P.Peers;
using TomP2P.Rpc;
using Buffer = TomP2P.Message.Buffer;

namespace TomP2P.P2P.Builder
{
    public class SendDirectBuilder : IConnectionConfiguration, ISendDirectBuilder, ISignatureBuilder<SendDirectBuilder>
    {
        // TODO find FutureDirect equivalent

        private readonly Peer _peer;
        private readonly PeerAddress _recipientAddress;

        private Buffer _buffer;



        public int IdleTcpSeconds
        {
            get { throw new NotImplementedException(); }
        }

        public int IdleUdpSeconds
        {
            get { throw new NotImplementedException(); }
        }

        public int ConnectionTimeoutTcpMillis
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsForceTcp
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsForceUdp
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsRaw
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsSign
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsStreaming
        {
            get { throw new NotImplementedException(); }
        }

        public Message.Buffer Buffer
        {
            get { throw new NotImplementedException(); }
        }

        public new object Object
        {
            get { throw new NotImplementedException(); }
        }

        public Extensions.Workaround.KeyPair KeyPair
        {
            get { throw new NotImplementedException(); }
        }


        public SendDirectBuilder SetSign()
        {
            throw new NotImplementedException();
        }

        public SendDirectBuilder SetSign(bool signMessage)
        {
            throw new NotImplementedException();
        }

        public SendDirectBuilder SetKeyPair(Extensions.Workaround.KeyPair keyPair)
        {
            throw new NotImplementedException();
        }
    }
}
