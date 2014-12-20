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

        public UdpServerSocket(IPEndPoint localEndPoint, int maxNrOfClients, int bufferSize)
            : base(localEndPoint, maxNrOfClients, bufferSize)
        {
            _serverSocket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        public override void Start()
        {
            Start(_serverSocket);
        }

        public override void Stop()
        {
            Stop(_serverSocket);
        }

        protected override async Task ServiceLoop(ClientToken token)
        {
            while (true)
            {
                // reset token for reuse
                token.Reset();

                // receive request from client
                token.RemotEndPoint = new IPEndPoint(IPAddress.Any, 0);
                await ReceiveAsync(token);

                // return / send back
                // TODO process data, maybe use abstract method
                Array.Copy(token.RecvBuffer, token.SendBuffer, BufferSize);

                await SendAsync(token);
            }
        }

        protected override async Task<int> SendAsync(ClientToken token)
        {
            // TODO correct endpoint??
            return await _serverSocket.SendToAsync(token.SendBuffer, 0, BufferSize, SocketFlags.None, token.RemotEndPoint);
        }

        protected override async Task<int> ReceiveAsync(ClientToken token)
        {
            // TODO correct endpoint?
            // TODO how can remoteEp be set to correct address without ref?
            var res = await _serverSocket.ReceiveFromAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None, token.RemotEndPoint);

            // TODO set remoteEp reference to result of task-output?
            token.RemotEndPoint = res.RemoteEp;

            return res.BytesReceived;
        }
    }
}
