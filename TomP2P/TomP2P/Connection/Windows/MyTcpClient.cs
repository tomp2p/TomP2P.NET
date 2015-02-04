using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Windows
{
    public class MyTcpClient : BaseChannel, ITcpClientChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly TcpClient _tcpClient;

        public MyTcpClient(IPEndPoint localEndPoint)
        {
            // bind
            _tcpClient = new TcpClient(localEndPoint);    
        }

        public Task ConnectAsync(IPEndPoint remoteEndPoint)
        {
            // just forward
            return _tcpClient.ConnectAsync(remoteEndPoint.Address, remoteEndPoint.Port);
        }

        public async Task SendMessageAsync(Message.Message message)
        {
            // execute outbound pipeline
            var writeRes = Pipeline.Write(message);
            Pipeline.ResetWrite();
            var bytes = ConnectionHelper.ExtractBytes(writeRes);

            // finally, send bytes over the wire
            var senderEp = ConnectionHelper.ExtractSenderEp(message);
            var receiverEp = _tcpClient.Client.RemoteEndPoint;
            Logger.Debug("Send TCP message {0}: Sender {1} --> Recipient {2}.", message, senderEp, receiverEp);

            await _tcpClient.GetStream().WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task ReceiveMessageAsync()
        {
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
                var piece = new StreamPiece(buf, (IPEndPoint)Socket.LocalEndPoint, (IPEndPoint)Socket.RemoteEndPoint);
                Logger.Debug("MyTcpClient received {0}.", piece);
                
                // execute inbound pipeline
                Pipeline.Read(piece);
                Pipeline.ResetRead();
            } while (!IsClosed && stream.DataAvailable); // attention: socket might have been already closed
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
