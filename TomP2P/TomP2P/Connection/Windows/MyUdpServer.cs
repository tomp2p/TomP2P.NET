using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    public class MyUdpServer
    {
        private readonly UdpClient _udpServerSocket;

        private readonly int _maxNrOfClients;
        private volatile bool _isStopped; // volatile!

        public MyUdpServer(IPEndPoint localEndPoint, int maxNrOfClients)
        {
            _maxNrOfClients = maxNrOfClients;
            _udpServerSocket = new UdpClient(localEndPoint);
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

                // TODO process data, maybe use abstract method
                var sampleBytes = new byte[10];

                // return / send back

                await _udpServerSocket.SendAsync(sampleBytes, sampleBytes.Length, remoteEndPoint);
            }
        }
    }
}
