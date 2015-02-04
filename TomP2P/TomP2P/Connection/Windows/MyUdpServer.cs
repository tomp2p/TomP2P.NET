using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Windows
{
    public class MyUdpServer : BaseServer, IUdpChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly UdpClient _udpServer;

        public MyUdpServer(IPEndPoint localEndPoint)
        {
            _udpServer = new UdpClient(localEndPoint);
        }

        public override void DoStart()
        {
            // nothing to start here
        }

        protected override void DoClose()
        {
            _udpServer.Close();
        }

        protected override async Task ServiceLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // receive request from client
                UdpReceiveResult udpRes = await _udpServer.ReceiveAsync().WithCancellation(ct);
                IPEndPoint remoteEndPoint = udpRes.RemoteEndPoint;

                // process content
                // server-side inbound pipeline
                var buf = AlternativeCompositeByteBuf.CompBuffer();
                buf.WriteBytes(udpRes.Buffer.ToSByteArray());
                var dgram = new DatagramPacket(buf, (IPEndPoint) Socket.LocalEndPoint, remoteEndPoint);
                Logger.Debug("Received {0}.", dgram);

                var readRes = Pipeline.Read(dgram);
                Pipeline.ResetRead();
                
                // server-side outbound pipeline
                var writeRes = Pipeline.Write(readRes);
                Pipeline.ResetWrite();
                var bytes = ConnectionHelper.ExtractBytes(writeRes);

                // return / send back
                await _udpServer.SendAsync(bytes, bytes.Length, remoteEndPoint);
            }
        }

        public override Socket Socket
        {
            get { return _udpServer.Client; }
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
