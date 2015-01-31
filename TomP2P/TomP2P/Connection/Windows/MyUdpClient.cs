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

        /// <summary>
        /// Executes the client-side outbound pipeline and sends message over the wire.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async override Task SendMessageAsync(Message.Message message)
        {
            // execute outbound pipeline
            var writeRes = Pipeline.Write(message);
            Pipeline.ResetWrite();
            var bytes = ConnectionHelper.ExtractBytes(writeRes);

            // finally, send bytes over the wire
            var senderEp = ConnectionHelper.ExtractSenderEp(message);
            var receiverEp = ConnectionHelper.ExtractReceiverEp(message);
            Logger.Debug("Send UDP message {0}: Sender {1} --> Recipient {2}.", message, senderEp, receiverEp);

            await _udpClient.SendAsync(bytes, bytes.Length, receiverEp);
        }

        /// <summary>
        /// Receives bytes from the remote host and executes the client-side inbound pipeline.
        /// </summary>
        /// <returns></returns>
        public async override Task ReceiveMessageAsync()
        {
            // receive bytes, create a datagram wrapper
            var udpRes = await _udpClient.ReceiveAsync();

            var buf = AlternativeCompositeByteBuf.CompBuffer();
            buf.WriteBytes(udpRes.Buffer.ToSByteArray());
            var dgram = new DatagramPacket(buf, (IPEndPoint) Socket.LocalEndPoint, udpRes.RemoteEndPoint);
            Logger.Debug("MyUdpClient received {0}.", dgram);

            // execute inbound pipeline
            Pipeline.Read(dgram);
            Pipeline.ResetRead();
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
            get { return !IsClosed; } // TODO ok?
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
