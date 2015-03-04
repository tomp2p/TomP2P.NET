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

        public MyTcpServer(IPEndPoint localEndPoint, Pipeline pipeline)
            : base(localEndPoint, pipeline)
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
                    object readRes;

                    // accept a client connection
                    var client = await _tcpServer.AcceptTcpClientAsync().WithCancellation(ct);
                    var stream = client.GetStream();
                    var pieceCount = 0;
                    var session = Pipeline.CreateNewServerSession();
                    session.TriggerActive();
                    
                    // process content
                    do
                    {
                        // TODO find zero-copy way, use same buffer
                        var nrBytes = await stream.ReadAsync(recvBuffer, 0, recvBuffer.Length, ct);
                        var buf = AlternativeCompositeByteBuf.CompBuffer();
                        buf.WriteBytes(recvBuffer.ToSByteArray(), 0, nrBytes);

                        LocalEndPoint = (IPEndPoint)client.Client.LocalEndPoint;
                        RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;

                        var piece = new StreamPiece(buf, LocalEndPoint, RemoteEndPoint);
                        Logger.Debug("[{0}] Received {1}. {2} : {3}", ++pieceCount, piece, Convenient.ToHumanReadable(nrBytes), Convenient.ToString(recvBuffer));

                        // execute inbound pipeline, per piece
                        readRes = session.Read(piece); // resets timeout
                        session.Reset(); // resets session internals
                    } while (!IsClosed && stream.DataAvailable && !session.IsTimedOut);

                    if (session.IsTimedOut)
                    {
                        // continue in service loop
                        continue;
                    }

                    // execute outbound pipeline
                    var writeRes = session.Write(readRes); // resets timeout
                    if (session.IsTimedOut)
                    {
                        // continue in service loop
                        continue;
                    }

                    // send back
                    var bytes = ConnectionHelper.ExtractBytes(writeRes);
                    await stream.WriteAsync(bytes, 0, bytes.Length, ct);
                    NotifyWriteCompleted(); // resets timeout
                    Logger.Debug("Sent {0} : {1}", Convenient.ToHumanReadable(bytes.Length), Convenient.ToString(bytes));

                    session.TriggerInactive();
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
