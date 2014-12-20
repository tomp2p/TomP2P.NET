using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class UdpClientSocket : AsyncClientSocket
    {
        private readonly Socket _udpClient;

        public UdpClientSocket(IPEndPoint localEndPoint)
        {
            _udpClient = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        public async Task<int> SendAsync(byte[] buffer, EndPoint remoteEndPoint)
        {
            return await _udpClient.SendToAsync(buffer, 0, buffer.Length, SocketFlags.None, remoteEndPoint);
        }

        public async Task<int> ReceiveAsync(byte[] buffer, EndPoint remoteEndPoint)
        {
            var res = await _udpClient.ReceiveFromAsync(buffer, 0, buffer.Length, SocketFlags.None, remoteEndPoint);
            return res.BytesReceived;
        }
    }
}
