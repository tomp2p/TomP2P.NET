using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Connection.NET_Helper;
using TomP2P.Message;

namespace TomP2P.Connection.Windows
{
    public class MyUdpServer
    {
        private readonly IPEndPoint _localEndPoint;
        private readonly int _maxNrOfClients;
        private readonly UdpClient _udpServerSocket;

        private readonly TomP2PSinglePacketUDP _decoder;
        private readonly TomP2POutbound _encoder;
        private readonly Dispatcher _dispatcher;

        private volatile bool _isStopped; // volatile!

        public MyUdpServer(IPEndPoint localEndPoint, int maxNrOfClients, TomP2PSinglePacketUDP decoder, 
            TomP2POutbound encoder, Dispatcher dispatcher)
        {
            _localEndPoint = localEndPoint;
            _maxNrOfClients = maxNrOfClients;
            _udpServerSocket = new UdpClient(localEndPoint);

            _decoder = decoder;
            _encoder = encoder;
            _dispatcher = dispatcher;
        }

        public void Start()
        {
            /* already done in c'tor
            // bind
            if (LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // set dual-mode (IPv4 & IPv6) for the socket listener
                // see http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                serverSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, LocalEndPoint.Port));
            }
            else
            {
                serverSocket.Bind(LocalEndPoint);
            }*/

            /*
            // listen, TCP-only
            if (serverSocket.ProtocolType == ProtocolType.Tcp)
            {
                serverSocket.Listen(MaxNrOfClients);
            }*/

            // accept MaxNrOfClients simultaneous connections
            for (int i = 0; i < _maxNrOfClients; i++)
            {
                ServiceLoopAsync();
            }
            _isStopped = false;
        }

        public void Stop()
        {
            _udpServerSocket.Close();
            _isStopped = true;
        }

        protected async Task ServiceLoopAsync()
        {
            while (!_isStopped)
            {
                // receive request from client
                UdpReceiveResult recvRes = await _udpServerSocket.ReceiveAsync();
                IPEndPoint remoteEndPoint = recvRes.RemoteEndPoint;

                // process data
                byte[] sendBytes = UdpPipeline(recvRes.Buffer, _localEndPoint, remoteEndPoint);

                // return / send back
                await _udpServerSocket.SendAsync(sendBytes, sendBytes.Length, remoteEndPoint);
            }
        }

        private byte[] UdpPipeline(byte[] recvBytes, IPEndPoint recipient, IPEndPoint sender)
        {
            // 1. decode incoming message
            // 2. hand it to the Dispatcher
            // 3. encode outgoing message
            var recvMessage = _decoder.Read(recvBytes, recipient, sender);

            // null means that no response is sent back
            // TODO does this mean that we can close channel?
            var responseMessage = _dispatcher.RequestMessageReceived(recvMessage, true, _udpServerSocket.Client);

            // TODO channel might have been closed, check

            var buffer = _encoder.Write(responseMessage);
            var sendBytes = ConnectionHelper.ExtractBytes(buffer);
            return sendBytes;
        }
    }
}
