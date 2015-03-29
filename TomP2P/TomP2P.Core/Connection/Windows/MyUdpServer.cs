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
        private readonly UdpClient _udpClient;

        public MyUdpServer(IPEndPoint localEndPoint, Pipeline pipeline)
            : base(localEndPoint, pipeline)
        {
            _udpClient = new UdpClient(LocalEndPoint);
            _udpClient.Client.EnableBroadcast = true;
        }

        protected override async Task ServiceLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // receive request from client
                    var udpRes = await _udpClient.ReceiveAsync().WithCancellation(ct);
                    RemoteEndPoint = udpRes.RemoteEndPoint;
                    ThreadPool.QueueUserWorkItem(async delegate
                    {
                        try
                        {
                            await ProcessRequestAsync(udpRes);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("An exception occurred during the UDP server's service loop.", ex);
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
                _udpClient.Close();
            }
        }

        protected override async Task ProcessRequestAsync(object state)
        {
            var udpRes = (UdpReceiveResult) state;

            // prepare new session
            var buf = AlternativeCompositeByteBuf.CompBuffer();
            var session = Pipeline.CreateNewServerSession(this);
            session.TriggerActive();

            // process content
            buf.WriteBytes(udpRes.Buffer.ToSByteArray());

            var localEp = (IPEndPoint)_udpClient.Client.LocalEndPoint;
            var remoteEp = udpRes.RemoteEndPoint;

            var dgram = new DatagramPacket(buf, localEp, remoteEp);
            Logger.Debug("Received {0}. {1} : {2}", dgram, Convenient.ToHumanReadable(udpRes.Buffer.Length),
                Convenient.ToString(udpRes.Buffer));

            // execute inbound pipeline
            var readRes = session.Read(dgram); // resets timeout
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
            await _udpClient.SendAsync(bytes, bytes.Length, remoteEp);
            NotifyWriteCompleted(); // resets timeout
            Logger.Debug("Sent {0} : {1}", Convenient.ToHumanReadable(udpRes.Buffer.Length),
                Convenient.ToString(udpRes.Buffer));

            session.TriggerInactive();
        }

        public override string ToString()
        {
            return String.Format("MyUdpServer ({0})", RuntimeHelpers.GetHashCode(this));
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
