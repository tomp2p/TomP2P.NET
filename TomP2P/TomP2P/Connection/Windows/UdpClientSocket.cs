using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;
using TomP2P.Message;

namespace TomP2P.Connection.Windows
{
    public class UdpClientSocket : AsyncClientSocket
    {
        private readonly Socket _udpClient;

        public UdpClientSocket(IPEndPoint localEndPoint)
        {
            _udpClient = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        public void Bind(EndPoint localEndPoint)
        {
            // TODO needed? should be same as in c'tor
            _udpClient.Bind(localEndPoint);
        }

        public async Task<int> SendAsync(Message.Message message, EndPoint remoteEndPoint)
        {
            var encoder = new Encoder(null);
            AlternativeCompositeByteBuf buf = AlternativeCompositeByteBuf.CompBuffer();
            encoder.Write(buf, message, null);
            var buffer = buf.NioBuffer();
            buffer.Position = 0;
            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes, 0, bytes.Length);
            return await SendAsync(bytes, remoteEndPoint);
        }

        public async Task<int> SendAsync(byte[] buffer, EndPoint remoteEndPoint)
        {
            return await _udpClient.SendToAsync(buffer, 0, buffer.Length, SocketFlags.None, remoteEndPoint);
        }

        public async Task<int> ReceiveAsync(byte[] buffer, EndPoint remoteEndPoint)
        {
            // TODO no binding needed? maybe when receiveFrom is called before sendTo
            // TODO also see http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.bind(v=vs.110).aspx
            var res = await _udpClient.ReceiveFromAsync(buffer, 0, buffer.Length, SocketFlags.None, remoteEndPoint);
            return res.BytesReceived;
        }

        public override void Close()
        {
            _udpClient.Close();
        }
    }
}
