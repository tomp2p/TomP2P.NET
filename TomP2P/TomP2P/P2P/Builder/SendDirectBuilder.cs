using System;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Extensions.Workaround;
using TomP2P.Futures;
using TomP2P.Peers;
using TomP2P.Rpc;
using Buffer = TomP2P.Message.Buffer;

namespace TomP2P.P2P.Builder
{
    public class SendDirectBuilder : IConnectionConfiguration, ISendDirectBuilder, ISignatureBuilder<SendDirectBuilder>
    {
        private static readonly TcsDirect FutureRequestShutdown = new TcsDirect("Peer is shutting down.");

        private readonly Peer _peer;
        public PeerAddress RecipientAddress { get; private set; }

        public Buffer Buffer { get; private set; }

        public TcsPeerConnection TcsRecipientConnection { get; private set; }
        public PeerConnection PeerConnection { get; private set; }

        private object _object;

        public Task<ChannelCreator> TaskChannelCreator { get; private set; }

        private bool _streaming = false;
        private bool _forceUdp = false;
        private bool _forceTcp = false;

        private KeyPair _keyPair = null;

        private int _idleTcpSeconds = ConnectionBean.DefaultTcpIdleSeconds;
        private int _idleUdpSeconds = ConnectionBean.DefaultUdpIdleSeconds;
        private int _connectionTimeoutTcpMillis = ConnectionBean.DefaultConnectionTimeoutTcp;

        public SendDirectBuilder(Peer peer, PeerAddress peerAddress)
        {
            _peer = peer;
            RecipientAddress = peerAddress;
            TcsRecipientConnection = null;
        }

        public SendDirectBuilder(Peer peer, TcsPeerConnection tcsPeerConnection)
        {
            _peer = peer;
            RecipientAddress = null;
            TcsRecipientConnection = tcsPeerConnection;
        }

        public SendDirectBuilder(Peer peer, PeerConnection peerConnection)
        {
            _peer = peer;
            RecipientAddress = null;
            PeerConnection = peerConnection;
        }

        public TcsDirect Start()
        {
            if (_peer.IsShutdown)
            {
                return FutureRequestShutdown;
            }

            bool keepAlive;
            PeerAddress remotePeer;
            if (RecipientAddress != null && TcsRecipientConnection == null)
            {
                keepAlive = false;
                remotePeer = RecipientAddress;
            }
            else if (RecipientAddress == null && TcsRecipientConnection != null)
            {
                keepAlive = true;
                remotePeer = TcsRecipientConnection.RemotePeer;
            }
            else if (PeerConnection != null)
            {
                keepAlive = true;
                remotePeer = PeerConnection.RemotePeer;
            }
            else
            {
                throw new ArgumentException("Either the recipient address or peer connection has to be set.");
            }

            if (TaskChannelCreator == null)
            {
                TaskChannelCreator = _peer.ConnectionBean.Reservation.CreateAsync(IsForceUdp ? 1 : 0, IsForceUdp ? 0 : 1);
            }

            var requestHandler = _peer.DirectDataRpc.SendInternal(remotePeer, this);
            if (keepAlive)
            {
                if (PeerConnection != null)
                {
                    SendDirectRequest(requestHandler, PeerConnection);
                }
                else
                {
                    TcsRecipientConnection.Task.ContinueWith(tpc =>
                    {
                        if (!tpc.IsFaulted)
                        {
                            SendDirectRequest(requestHandler, TcsRecipientConnection.PeerConnection);
                        }
                        else
                        {
                            requestHandler.TcsResponse.SetException(new TaskFailedException("Could not acquire channel (1).", tpc));
                        }
                    });
                }
            }
            else
            {
                Utils.Utils.AddReleaseListener(TaskChannelCreator, requestHandler.TcsResponse.Task);
                TaskChannelCreator.ContinueWith(tcc =>
                {
                    if (!tcc.IsFaulted)
                    {
                        requestHandler.SendTcpAsync(tcc.Result);
                    }
                    else
                    {
                        requestHandler.TcsResponse.SetException(new TaskFailedException("Could not create channel.", tcc));
                    }
                });
            }

            return new TcsDirect(requestHandler.TcsResponse);
        }

        private static void SendDirectRequest(RequestHandler requestHandler, PeerConnection peerConnection)
        {
            var taskCc = peerConnection.AcquireAsync(requestHandler.TcsResponse);
            taskCc.ContinueWith(tcc =>
            {
                if (!tcc.IsFaulted)
                {
                    var requestMessage = requestHandler.TcsResponse.Task.AsyncState as Message.Message;
                    requestMessage.SetKeepAlive(true);
                    requestHandler.SendTcpAsync(peerConnection.ChannelCreator, peerConnection);
                }
                else
                {
                    requestHandler.TcsResponse.SetException(new TaskFailedException("Could not acquire channel (2).", tcc));
                }
            });
        }

        public SendDirectBuilder SetBuffer(Buffer buffer)
        {
            Buffer = buffer;
            return this;
        }

        public SendDirectBuilder SetTcsPeerConnection(TcsPeerConnection tcsPeerConnection)
        {
            TcsRecipientConnection = tcsPeerConnection;
            return this;
        }

        public SendDirectBuilder SetPeerConnection(PeerConnection peerConnection)
        {
            PeerConnection = peerConnection;
            return this;
        }

        public SendDirectBuilder SetTcsChannelCreator(Task<ChannelCreator> taskChannelCreator)
        {
            TaskChannelCreator = taskChannelCreator;
            return this;
        }

        public int IdleTcpSeconds
        {
            get { return _idleTcpSeconds; }
        }

        public SendDirectBuilder SetIdleTcpSeconds(int idleTcpSeconds)
        {
            _idleTcpSeconds = idleTcpSeconds;
            return this;
        }

        public int IdleUdpSeconds
        {
            get { return _idleUdpSeconds; }
        }

        public SendDirectBuilder SetIdleUdpSeconds(int idleUdpSeconds)
        {
            _idleUdpSeconds = idleUdpSeconds;
            return this;
        }

        public int ConnectionTimeoutTcpMillis
        {
            get { return _connectionTimeoutTcpMillis; }
        }

        public SendDirectBuilder SetConnectionTimeoutTcpMillis(int connectionTimeoutTcpMillis)
        {
            _connectionTimeoutTcpMillis = connectionTimeoutTcpMillis;
            return this;
        }

        public bool IsForceTcp
        {
            get { return _forceTcp; }
        }

        public SendDirectBuilder SetIsForceTcp()
        {
            return SetIsForceTcp(true);
        }

        public SendDirectBuilder SetIsForceTcp(bool forceTcp)
        {
            _forceTcp = forceTcp;
            return this;
        }

        public bool IsForceUdp
        {
            get { return _forceUdp; }
        }

        public SendDirectBuilder SetIsForceUdp()
        {
            return SetIsForceUdp(true);
        }

        public SendDirectBuilder SetIsForceUdp(bool forceUdp)
        {
            _forceUdp = forceUdp;
            return this;
        }

        public bool IsRaw
        {
            get { return _object == null; }
        }

        public bool IsSign
        {
            get { return _keyPair != null; }
        }

        public bool IsStreaming
        {
            get { return _streaming; }
        }

        public SendDirectBuilder SetIsStreaming()
        {
            return SetIsStreaming(true);
        }

        public SendDirectBuilder SetIsStreaming(bool streaming)
        {
            _streaming = streaming;
            return this;
        }

        public object Object
        {
            get { return _object; }
        }

        public SendDirectBuilder SetObject(object obj)
        {
            _object = obj;
            return this;
        }

        public KeyPair KeyPair
        {
            get { return _keyPair; }
        }


        public SendDirectBuilder SetSign()
        {
            _keyPair = _peer.PeerBean.KeyPair;
            return this;
        }

        public SendDirectBuilder SetSign(bool signMessage)
        {
            if (signMessage)
            {
                SetSign();
            }
            else
            {
                _keyPair = null;
            }
            return this;
        }

        public SendDirectBuilder SetKeyPair(KeyPair keyPair)
        {
            _keyPair = keyPair;
            return this;
        }
    }
}
