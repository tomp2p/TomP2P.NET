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

        // wrapped member
        private readonly TcpListener _tcpListener;

        public MyTcpServer(IPEndPoint localEndPoint, Pipeline pipeline)
            : base(localEndPoint, pipeline)
        {
            _tcpListener = new TcpListener(LocalEndPoint);
            _tcpListener.Server.LingerState = new LingerOption(false, 0);
            _tcpListener.Server.NoDelay = true;
        }

        protected override async Task ServiceLoopAsync(CancellationToken ct)
        {
            _tcpListener.Start();
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // accept a client connection
                    var client = await _tcpListener.AcceptTcpClientAsync().WithCancellation(ct);
                    RemoteEndPoint = (IPEndPoint) client.Client.RemoteEndPoint;
                    ThreadPool.QueueUserWorkItem(async delegate
                    {
                        try
                        {
                            await ProcessRequestAsync(client);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("An exception occurred during the TCP server's service loop.", ex);
                            throw;
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // the server has been stopped -> stop service loop
            }
            finally
            {
                _tcpListener.Stop();
            }
        }

        protected override async Task ProcessRequestAsync(object state)
        {
            var client = (TcpClient) state;
            var stream = client.GetStream();

            object readRes;
            var pieceCount = 0;

            // prepare new session
            var recvBuffer = new byte[256];
            var buf = AlternativeCompositeByteBuf.CompBuffer();
            var session = Pipeline.CreateNewServerSession(this);
            session.TriggerActive();

            // process content
            do
            {
                // TODO find zero-copy way, use same buffer
                Array.Clear(recvBuffer, 0, recvBuffer.Length);
                buf.Clear();
                var nrBytes = await stream.ReadAsync(recvBuffer, 0, recvBuffer.Length);
                buf.WriteBytes(recvBuffer.ToSByteArray(), 0, nrBytes);

                var localEp = (IPEndPoint) client.Client.LocalEndPoint;
                var remoteEp = (IPEndPoint) client.Client.RemoteEndPoint;

                var piece = new StreamPiece(buf, localEp, remoteEp);
                Logger.Debug("[{0}] Received {1}. {2} : {3}", ++pieceCount, piece,
                    Convenient.ToHumanReadable(nrBytes), Convenient.ToString(recvBuffer));

                // execute inbound pipeline, per piece
                readRes = session.Read(piece); // resets timeout
                session.Reset(); // resets session internals
            } while (!IsClosed && stream.DataAvailable && !session.IsTimedOut);

            if (session.IsTimedOut)
            {
                session.TriggerInactive();
                return;
            }

            // execute outbound pipeline
            var writeRes = session.Write(readRes); // resets timeout
            if (session.IsTimedOut)
            {
                session.TriggerInactive();
                return;
            }

            // send back
            var bytes = ConnectionHelper.ExtractBytes(writeRes);
            await stream.WriteAsync(bytes, 0, bytes.Length);
            NotifyWriteCompleted(); // resets timeout
            Logger.Debug("Sent {0} : {1}", Convenient.ToHumanReadable(bytes.Length), Convenient.ToString(bytes));

            session.TriggerInactive();
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
