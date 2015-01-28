using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Connection.NET_Helper;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Message;

namespace TomP2P.Connection.Windows
{
    public class MyUdpServer : BaseChannel, IUdpChannel
    {
        // wrapped member
        private readonly UdpClient _udpServer;

        private readonly TomP2PSinglePacketUdp _decoder;
        private readonly TomP2POutbound _encoder;
        private readonly Dispatcher _dispatcher;

        private volatile bool _isStopped; // volatile!

        public MyUdpServer(IPEndPoint localEndPoint, TomP2PSinglePacketUdp decoder, 
            TomP2POutbound encoder, Dispatcher dispatcher)
        {
            _udpServer = new UdpClient(localEndPoint);

            _decoder = decoder;
            _encoder = encoder;
            _dispatcher = dispatcher;
        }

        public void Start()
        {
           // accept MaxNrOfClients simultaneous connections
            for (int i = 0; i < Utils.Utils.GetMaxNrOfClients(); i++)
            {
                ServiceLoopAsync();
            }
            _isStopped = false;
        }

        public void Stop()
        {
            this.Close();
        }

        protected override void DoClose()
        {
            // TODO notify async wait in service loop (CancellationToken)
            _udpServer.Close();
            _isStopped = true;
        }

        protected async Task ServiceLoopAsync()
        {
            while (!_isStopped)
            {
                // receive request from client
                UdpReceiveResult recvRes = await _udpServer.ReceiveAsync();
                IPEndPoint remoteEndPoint = recvRes.RemoteEndPoint;

                // process data
                byte[] sendBytes = UdpPipeline(recvRes.Buffer, _udpServer.Client.LocalEndPoint as IPEndPoint, remoteEndPoint);

                // return / send back
                await _udpServer.SendAsync(sendBytes, sendBytes.Length, remoteEndPoint);
            }
        }

        private byte[] UdpPipeline(byte[] recvBytes, IPEndPoint recipient, IPEndPoint sender)
        {
            // TODO implement a pipeline config somewhat similar to Java's ChannelServer.handlers()
            // TODO this method then just executes the pipeline and guarantees the flow

            // 1. decode incoming message
            // 2. hand it to the Dispatcher
            // 3. encode outgoing message
            var recvMessage = _decoder.Read(recvBytes, recipient, sender);

            // null means that no response is sent back
            // TODO does this mean that we can close channel?
            var responseMessage = _dispatcher.RequestMessageReceived(recvMessage, true, Socket);

            // TODO channel might have been closed, check

            var buffer = _encoder.Write(responseMessage);
            var sendBytes = ConnectionHelper.ExtractBytes(buffer);
            return sendBytes;
        }

        public bool IsOpen
        {
            get { throw new System.NotImplementedException(); }
        }

        public override Socket Socket
        {
            get { return _udpServer.Client; }
        }

        public override bool IsUdp
        {
            get { return true; }
        }

        public override bool IsTcp
        {
            get { return false; }
        }
    }
}
