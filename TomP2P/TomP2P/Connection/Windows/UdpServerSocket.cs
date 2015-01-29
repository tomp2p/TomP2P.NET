using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class UdpServerSocket : AsyncServerSocket
    {
        private readonly Socket _serverSocket;
        private readonly IPEndPoint _remoteEndPoint;
        
        public UdpServerSocket(IPEndPoint localEndPoint, int maxNrOfClients, int bufferSize)
            : base(localEndPoint, maxNrOfClients, bufferSize)
        {
            _serverSocket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            // set remoteEndPoint according to localEndPoint address-family
            if (localEndPoint.AddressFamily == AddressFamily.InterNetwork)
            {
                _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            } else if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _remoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            }
        }

        public override void Start()
        {
            Start(_serverSocket);
        }

        public override void Stop()
        {
            Stop(_serverSocket);
        }

        protected override async Task ServiceLoopAsync(ClientToken token)
        {
            while (!IsStopped)
            {
                // reset token for reuse
                token.Reset();

                // receive request from client
                token.RemotEndPoint = _remoteEndPoint;
                await ReceiveAsync(token);

                // return / send back
                // TODO process content, maybe use abstract method
                Array.Copy(token.RecvBuffer, token.SendBuffer, BufferSize);

                await SendAsync(token);
            }
        }

        protected override async Task<int> SendAsync(ClientToken token)
        {
            return await _serverSocket.SendToAsync(token.SendBuffer, 0, BufferSize, SocketFlags.None, token.RemotEndPoint);
        }

        protected override async Task<int> ReceiveAsync(ClientToken token)
        {
            var res = await _serverSocket.ReceiveFromAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None, token.RemotEndPoint);

            token.RemotEndPoint = res.RemoteEp;
            return res.BytesReceived;
        }
    }
}
