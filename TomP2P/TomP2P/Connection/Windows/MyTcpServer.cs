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
    public class MyTcpServer : BaseChannel, ITcpChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly TcpListener _tcpServer;

        private volatile bool _isStopped; // volatile!

        public MyTcpServer(IPEndPoint localEndPoint)
        {
            // local endpoint
            _tcpServer = new TcpListener(localEndPoint);
        }

        public void Start()
        {
            _tcpServer.Start();

            // accept MaxNrOfClients simultaneous connections
            var maxNrOfClients = Utils.Utils.GetMaxNrOfClients();
            for (int i = 0; i < maxNrOfClients; i++)
            {
                // TODO find a way to await -> exceptions
                ServiceLoopAsync();
            }
            _isStopped = false;
        }

        public void Stop()
        {
            Close();
        }

        protected override void DoClose()
        {
            _tcpServer.Stop();
            // TODO notify async wait in service loop (CancellationToken)
            _isStopped = true;
        }

        protected async Task ServiceLoopAsync()
        {
            while (!_isStopped)
            {
                // buffers
                var recvBuffer = new byte[256];
                var buf = AlternativeCompositeByteBuf.CompBuffer();
                object readRes;

                // accept a client connection
                var client = await _tcpServer.AcceptTcpClientAsync();
                var stream = client.GetStream();
                do
                {
                    // TODO find zero-copy way
                    var nrBytes = await stream.ReadAsync(recvBuffer, 0, recvBuffer.Length);
                    buf.Deallocate();
                    buf.WriteBytes(recvBuffer.ToSByteArray(), 0, nrBytes);
                    var piece = new StreamPiece(buf, (IPEndPoint) client.Client.LocalEndPoint, (IPEndPoint)client.Client.RemoteEndPoint);
                    Logger.Debug("MyTcpServer received {0}.", piece);

                    // execute inbound pipeline
                    readRes = Pipeline.Read(piece);
                    Pipeline.ResetRead();
                } while (stream.DataAvailable);

                // server-side outbound pipeline
                var writeRes = Pipeline.Write(readRes);
                Pipeline.ResetWrite();
                var bytes = ConnectionHelper.ExtractBytes(writeRes);

                // send back
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        public override Socket Socket
        {
            get { return _tcpServer.Server; }
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
