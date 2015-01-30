using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Windows
{
    public class MyUdpServer : BaseChannel, IUdpChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly UdpClient _udpServer;

        private volatile bool _isStopped; // volatile!

        public MyUdpServer(IPEndPoint localEndPoint)
        {
            _udpServer = new UdpClient(localEndPoint);
        }

        public void Start()
        {
           // accept MaxNrOfClients simultaneous connections
            var maxNrOfClients = Utils.Utils.GetMaxNrOfClients();
            for (int i = 0; i < maxNrOfClients; i++)
            {
                ServiceLoopAsync();
            }
            _isStopped = false;
        }

        public void Stop()
        {
            Close();
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
                UdpReceiveResult udpRes = await _udpServer.ReceiveAsync();
                IPEndPoint remoteEndPoint = udpRes.RemoteEndPoint;

                // process content
                // server-side inbound pipeline
                var buf = AlternativeCompositeByteBuf.CompBuffer();
                buf.WriteBytes(udpRes.Buffer.ToSByteArray());
                var dgram = new DatagramPacket(buf, Socket.LocalEndPoint as IPEndPoint, remoteEndPoint);
                Logger.Debug("MyUdpServer received {0}.", dgram);

                var obj = Pipeline.Read(dgram);
                
                // server-side outbound pipeline
                var bytes = Pipeline.Write(obj);
                Pipeline.Reset();

                // return / send back
                await _udpServer.SendAsync(bytes, bytes.Length, remoteEndPoint);
            }
        }

        /*private byte[] UdpPipeline(byte[] recvBytes, IPEndPoint recipient, IPEndPoint sender)
        {
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
        }*/

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
