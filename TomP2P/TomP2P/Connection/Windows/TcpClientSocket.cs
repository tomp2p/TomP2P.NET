using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class TcpClientSocket : AsyncClientSocket
    {
        public TcpClientSocket(IPEndPoint localEndPoint) 
            : base(localEndPoint)
        {
        }

        protected override Socket CreateClientSocket(AddressFamily addressFamily)
        {
            return new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public override async Task<int> Send(byte[] buffer)
        {
            return await ClientSocket.SendAsync(buffer, 0, buffer.Length, SocketFlags.None);

            // TODO TCP shutdown/close needed?
        }

        public override async Task<int> Receive(byte[] buffer)
        {
            return await ClientSocket.ReceiveAsync(buffer, 0, buffer.Length, SocketFlags.None);

            // TODO loop as long as recvBytes == 0?
            // TODO shutdown/close needed?
        }
    }
}
