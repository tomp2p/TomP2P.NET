using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Storage;

namespace TomP2P.Connection.Windows
{
    public class MyUdpClient : BaseClient, IUdpClientChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly UdpClient _udpClient;

        public MyUdpClient(IPEndPoint localEndPoint, Pipeline pipeline)
            : base(localEndPoint, pipeline)
        {
            // bind
            _udpClient = new UdpClient(localEndPoint);
            Logger.Info("Instantiated with object identity: {0}.", RuntimeHelpers.GetHashCode(this));
        }

        public override async Task SendBytesAsync(byte[] bytes, IPEndPoint senderEp, IPEndPoint receiverEp = null)
        {
            // send bytes
            await _udpClient.SendAsync(bytes, bytes.Length, receiverEp);
            Logger.Debug("Sent UDP: Sender {0} --> Recipient {1}. {2} : {3}", senderEp, receiverEp,
                Convenient.ToHumanReadable(bytes.Length), Convenient.ToString(bytes));
        }

        public override async Task DoReceiveMessageAsync()
        {
            // receive bytes
            UdpReceiveResult udpRes;
            try
            {
                udpRes = await _udpClient.ReceiveAsync().WithCancellation(CloseToken);

            }
            catch (OperationCanceledException)
            {
                // the socket has been closed
                return;
            }

            var buf = AlternativeCompositeByteBuf.CompBuffer();
            buf.WriteBytes(udpRes.Buffer.ToSByteArray());

            LocalEndPoint = (IPEndPoint)Socket.LocalEndPoint;
            RemoteEndPoint = udpRes.RemoteEndPoint;

            var dgram = new DatagramPacket(buf, LocalEndPoint, RemoteEndPoint);
            Logger.Debug("Received {0}. {1} : {2}", dgram, Convenient.ToHumanReadable(udpRes.Buffer.Length), Convenient.ToString(udpRes.Buffer));

            // execute inbound pipeline
            if (Session.IsTimedOut)
            {
                return;
            }
            Session.Read(dgram);
            Session.Reset();
        }

        protected override void DoClose()
        {
            base.DoClose();
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
