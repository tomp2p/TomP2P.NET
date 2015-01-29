using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Slightly extended <see cref="UdpClient"/>.
    /// </summary>
    public class MyUdpClient : BaseChannel, IUdpChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly UdpClient _udpClient;

        public MyUdpClient(IPEndPoint localEndPoint)
        {
            // bind
            _udpClient = new UdpClient(localEndPoint);    
        }

        public async Task SendAsync(Message.Message message)
        {
            // TODO check if works
            // execute outbound pipeline
            var bytes = Pipeline.Write(message);

            // finally, send bytes over the wire
            var senderEp = ConnectionHelper.ExtractSenderEp(message);
            var receiverEp = ConnectionHelper.ExtractReceiverEp(message);
            Logger.Debug("Send UDP message {0}: Sender {1} --> Recipient {2}.", message, senderEp, receiverEp);

            await _udpClient.SendAsync(bytes, bytes.Length, receiverEp);
        }

        public async Task ReceiveAsync()
        {
            // receive bytes, create a datagram wrapper
            var udpRes = await _udpClient.ReceiveAsync();

            ByteBuf buf = AlternativeCompositeByteBuf.CompBuffer();
            buf.WriteBytes(udpRes.Buffer.ToSByteArray());
            // TODO correct?
            var dgram = new DatagramPacket(buf, Socket.LocalEndPoint as IPEndPoint, udpRes.RemoteEndPoint);

            // execute inbound pipeline
            Pipeline.Read(dgram);
        }

        protected override void DoClose()
        {
            _udpClient.Close();
        }

        public override Socket Socket
        {
            get { return _udpClient.Client; }
        }

        public bool IsOpen
        {
            get { throw new NotImplementedException(); }
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
