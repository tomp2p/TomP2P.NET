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
    public class MyUdpServer : BaseServer, IUdpServerChannel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // wrapped member
        private readonly UdpClient _udpServer;

        public MyUdpServer(IPEndPoint localEndPoint, Pipeline pipeline)
            : base(localEndPoint, pipeline)
        {
            _udpServer = new UdpClient(localEndPoint);

            Logger.Info("Instantiated with object identity: {0}.", RuntimeHelpers.GetHashCode(this));
        }

        public override void DoStart()
        {
            // nothing to start here
        }

        protected override void DoClose()
        {
            _udpServer.Close();
        }

        public override async Task ServiceLoopAsync(CancellationToken ct)
        {
            PipelineSession session = null;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // receive request from client
                    UdpReceiveResult udpRes = await _udpServer.ReceiveAsync().WithCancellation(ct);
                    session = Pipeline.CreateNewServerSession(this);
                    session.TriggerActive();

                    // process content
                    var buf = AlternativeCompositeByteBuf.CompBuffer();
                    buf.WriteBytes(udpRes.Buffer.ToSByteArray());

                    LocalEndPoint = (IPEndPoint) Socket.LocalEndPoint;
                    RemoteEndPoint = udpRes.RemoteEndPoint;

                    var dgram = new DatagramPacket(buf, LocalEndPoint, RemoteEndPoint);
                    Logger.Debug("Received {0}. {1} : {2}", dgram, Convenient.ToHumanReadable(udpRes.Buffer.Length),
                        Convenient.ToString(udpRes.Buffer));

                    // execute inbound pipeline
                    var readRes = session.Read(dgram); // resets timeout
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
                    await _udpServer.SendAsync(bytes, bytes.Length, RemoteEndPoint);
                    NotifyWriteCompleted(); // resets timeout
                    Logger.Debug("Sent {0} : {1}", Convenient.ToHumanReadable(udpRes.Buffer.Length),
                        Convenient.ToString(udpRes.Buffer));

                    session.TriggerInactive();
                }
            }
            catch (OperationCanceledException)
            {
                // the server has been stopped -> stop service loop
            }
            finally
            {
                if (session != null)
                {
                    session.TriggerInactive();
                }
            }
        }

        public override string ToString()
        {
            return String.Format("MyUdpServer ({0})", RuntimeHelpers.GetHashCode(this));
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
