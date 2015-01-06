using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Connection.Pipeline;
using TomP2P.Extensions;
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

        // TODO make async
        public Task Write(Message.Message message, ChannelClientConfiguration channelClientConfiguration)
        {
            // work through client-side pipeline
            // 1. encoder (TomP2POutbound)
            var outbound = new TomP2POutbound(false, channelClientConfiguration.SignatureFactory);
            Context context = outbound.Write(message, true);

            // 2. send over the wire
            var buffer = context.MessageBuffer.NioBuffer();
            buffer.Position = 0;
            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes, 0, bytes.Length);
            return SendAsync(bytes, context.UdpRecipient);
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
            OnClosed();
        }
    }
}
