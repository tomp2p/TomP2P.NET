using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class UdpServerSocket : AsyncServerSocket
    {
        private EndPoint _remoteEp = new IPEndPoint(IPAddress.Any, 0);

        public UdpServerSocket(IPEndPoint localEndPoint, int maxNrOfClients, int bufferSize)
            : base(localEndPoint, maxNrOfClients, bufferSize)
        {
        }

        protected override Socket CreateServerSocket()
        {
            return new Socket(LocalEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        protected override async Task<int> Send(ClientToken token)
        {
            // TODO correct endpoint??
            return await token.ClientHandler.SendToAsync(token.SendBuffer, 0, BufferSize, SocketFlags.None, _remoteEp);
        }

        protected override async Task<int> Receive(ClientToken token)
        {
            // TODO correct endpoint?
            // TODO how can remoteEp be set to correct address without ref?
            return await token.ClientHandler.ReceiveFromAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None, _remoteEp);
        }

        protected override void CloseHandlerSocket(Socket handler)
        {
            throw new NotImplementedException();
        }
    }
}
