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
    public class MyTcpClient : BaseChannel, ITcpClientChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly TcpClient _tcpClient;

        public MyTcpClient(IPEndPoint localEndPoint)
            : base(localEndPoint)
        {
            // bind
            _tcpClient = new TcpClient(localEndPoint);

            Logger.Info("Instantiated with object identity: {0}.", RuntimeHelpers.GetHashCode(this));
        }

        public Task ConnectAsync(IPEndPoint remoteEndPoint)
        {
            return _tcpClient.ConnectAsync(remoteEndPoint.Address, remoteEndPoint.Port);
        }

        public async Task SendMessageAsync(Message.Message message)
        {
            var session = Pipeline.GetNewSession();

            // execute outbound pipeline
            var writeRes = session.Write(message);
            var bytes = ConnectionHelper.ExtractBytes(writeRes);

            // finally, send bytes over the wire
            var senderEp = ConnectionHelper.ExtractSenderEp(message);
            var receiverEp = _tcpClient.Client.RemoteEndPoint;
            Logger.Debug("Send TCP message {0}: Sender {1} --> Recipient {2}.", message, senderEp, receiverEp);

            await _tcpClient.GetStream().WriteAsync(bytes, 0, bytes.Length);
            NotifyWriteCompleted();

            Pipeline.ReleaseSession(session);
        }

        public async Task ReceiveMessageAsync()
        {
            var session = Pipeline.GetNewSession();

            // receive bytes
            var bytesRecv = new byte[256];

            var buf = AlternativeCompositeByteBuf.CompBuffer();
            var stream = _tcpClient.GetStream();
            do
            {
                // TODO find zero-copy way
                var nrBytes = await stream.ReadAsync(bytesRecv, 0, bytesRecv.Length);
                buf.Deallocate();
                buf.WriteBytes(bytesRecv.ToSByteArray(), 0, nrBytes);

                LocalEndPoint = (IPEndPoint)Socket.LocalEndPoint;
                RemoteEndPoint = (IPEndPoint)Socket.RemoteEndPoint;

                var piece = new StreamPiece(buf, LocalEndPoint, RemoteEndPoint);
                Logger.Debug("Received {0}.", piece);

                // execute inbound pipeline
                session.Read(piece);
            } while (!IsClosed && stream.DataAvailable);

            Pipeline.ReleaseSession(session);
        }

        protected override void DoClose()
        {
            _tcpClient.Close();
        }

        public override Socket Socket
        {
            get { return _tcpClient.Client; }
        }

        public override bool IsUdp
        {
            get { return false; }
        }

        public override bool IsTcp
        {
            get { return true; }
        }
    }
}
