using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Storage;

namespace TomP2P.Connection.Windows
{
    public class MyUdpClient : BaseChannel, IUdpClientChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly UdpClient _udpClient;

        public MyUdpClient(IPEndPoint localEndPoint)
            : base(localEndPoint)
        {
            // bind
            _udpClient = new UdpClient(localEndPoint);
            Logger.Info("Instantiated with object identity: {0}.", RuntimeHelpers.GetHashCode(this));
        }

        public async Task SendMessageAsync(Message.Message message)
        {
            var session = Pipeline.GetNewSession();

            // execute outbound pipeline
            var writeRes = session.Write(message);
            var bytes = ConnectionHelper.ExtractBytes(writeRes);

            // finally, send bytes over the wire
            var senderEp = ConnectionHelper.ExtractSenderEp(message);
            var receiverEp = ConnectionHelper.ExtractReceiverEp(message);
            Logger.Debug("Send UDP message {0}: Sender {1} --> Recipient {2}.", message, senderEp, receiverEp);

            Logger.Debug("Sending bytes {0}.", Convenient.PrintByteArray(bytes));
            await _udpClient.SendAsync(bytes, bytes.Length, receiverEp);
            NotifyWriteCompleted();

            Pipeline.ReleaseSession(session);
        }

        public async Task ReceiveMessageAsync()
        {
            // TODO check necessity of new session (handlers set in sender) (2x)
            var session = Pipeline.GetNewSession();

            // receive bytes, create a datagram wrapper
            var udpRes = await _udpClient.ReceiveAsync();

            var buf = AlternativeCompositeByteBuf.CompBuffer();
            buf.WriteBytes(udpRes.Buffer.ToSByteArray());

            LocalEndPoint = (IPEndPoint) Socket.LocalEndPoint;
            RemoteEndPoint = udpRes.RemoteEndPoint;

            var dgram = new DatagramPacket(buf, LocalEndPoint, RemoteEndPoint);
            Logger.Debug("Received {0}.", dgram);

            // execute inbound pipeline
            session.Read(dgram);

            Pipeline.ReleaseSession(session);
        }

        protected override void DoClose()
        {
            _udpClient.Close();
        }

        public override string ToString()
        {
            return String.Format("MyUdpClient ({0})", RuntimeHelpers.GetHashCode(this));
        }

        public override Socket Socket
        {
            get { return _udpClient.Client; }
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
