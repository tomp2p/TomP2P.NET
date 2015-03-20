using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Core.Connection.Windows.Netty;
using TomP2P.Core.Storage;
using TomP2P.Extensions;

namespace TomP2P.Core.Connection.Windows
{
    public class MyTcpServer : BaseServer, ITcpServerChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MyTcpServer(IPEndPoint localEndPoint, Pipeline pipeline)
            : base(localEndPoint, pipeline)
        {
            //Logger.Info("Instantiated with object identity: {0}.", RuntimeHelpers.GetHashCode(this));
        }

        public override async Task ServiceLoopAsync(CancellationToken ct)
        {
            var tcpListener = new TcpListener(LocalEndPoint);
            tcpListener.Server.LingerState = new LingerOption(false, 0); // TODO correct?
            tcpListener.Server.NoDelay = true; // TODO correct?
            tcpListener.Start();
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // buffers
                    var recvBuffer = new byte[256];
                    var buf = AlternativeCompositeByteBuf.CompBuffer();
                    object readRes;

                    // accept a client connection
                    var client = await tcpListener.AcceptTcpClientAsync().WithCancellation(ct);
                    var stream = client.GetStream();
                    var pieceCount = 0;
                    var session = Pipeline.CreateNewServerSession(this);
                    session.TriggerActive();

                    // process content
                    do
                    {
                        // TODO find zero-copy way, use same buffer
                        var nrBytes = await stream.ReadAsync(recvBuffer, 0, recvBuffer.Length, ct);
                        buf.Clear();
                        buf.WriteBytes(recvBuffer.ToSByteArray(), 0, nrBytes);

                        LocalEndPoint = (IPEndPoint) client.Client.LocalEndPoint;
                        RemoteEndPoint = (IPEndPoint) client.Client.RemoteEndPoint;

                        var piece = new StreamPiece(buf, LocalEndPoint, RemoteEndPoint);
                        Logger.Debug("[{0}] Received {1}. {2} : {3}", ++pieceCount, piece,
                            Convenient.ToHumanReadable(nrBytes), Convenient.ToString(recvBuffer));

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
            finally
            {
                tcpListener.Stop();
            }
        }

        public override string ToString()
        {
            return String.Format("MyTcpServer ({0})", RuntimeHelpers.GetHashCode(this));
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
