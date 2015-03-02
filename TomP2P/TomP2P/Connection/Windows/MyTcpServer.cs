using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Storage;

namespace TomP2P.Connection.Windows
{
    public class MyTcpServer : BaseServer, ITcpServerChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly TcpListener _tcpServer;

        public MyTcpServer(IPEndPoint localEndPoint)
            : base(localEndPoint)
        {
            // local endpoint
            _tcpServer = new TcpListener(localEndPoint);

            Logger.Info("Instantiated with object identity: {0}.", RuntimeHelpers.GetHashCode(this));
        }

        public override void DoStart()
        {
            _tcpServer.Start();
        }

        protected override void DoClose()
        {
            _tcpServer.Stop();
        }

        public override async Task ServiceLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // buffers
                    var recvBuffer = new byte[256];
                    var buf = AlternativeCompositeByteBuf.CompBuffer();
                    object readRes;

                    // accept a client connection
                    var client = await _tcpServer.AcceptTcpClientAsync().WithCancellation(ct);
                    var stream = client.GetStream();
                    var session = Pipeline.GetNewSession();
                    do
                    {
                        // TODO find zero-copy way
                        var nrBytes = await stream.ReadAsync(recvBuffer, 0, recvBuffer.Length, ct);
                        buf.Deallocate();
                        buf.WriteBytes(recvBuffer.ToSByteArray(), 0, nrBytes);

                        LocalEndPoint = (IPEndPoint)client.Client.LocalEndPoint;
                        RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;

                        var piece = new StreamPiece(buf, LocalEndPoint, RemoteEndPoint);
                        Logger.Debug("Received {0}.", piece);

                        // execute inbound pipeline
                        readRes = session.Read(piece);
                    } while (!IsClosed && stream.DataAvailable);

                    // server-side outbound pipeline
                    var writeRes = session.Write(readRes);
                    var bytes = ConnectionHelper.ExtractBytes(writeRes);

                    // send back
                    await stream.WriteAsync(bytes, 0, bytes.Length, ct);
                    NotifyWriteCompleted();

                    Pipeline.ReleaseSession(session);
                }
            }
            catch (OperationCanceledException)
            {
                // the server has been stopped -> stop service loop
            }
        }

        public override string ToString()
        {
            return String.Format("MyTcpServer ({0})", RuntimeHelpers.GetHashCode(this));
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
