using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Slightly extended <see cref="UdpClient"/>.
    /// </summary>
    public class MyUdpClient : BaseChannel, IUdpChannel
    {
        // wrapped member
        private readonly UdpClient _udpClient;

        public MyUdpClient(IPEndPoint localEndPoint, Pipeline pipeline)
            : base(pipeline)
        {
            // bind
            _udpClient = new UdpClient(localEndPoint);    
        }

        public Task SendAsync(Message.Message message, IPEndPoint receiverEndPoint)
        {
            // TODO check if works
            var tcs = new TaskCompletionSource<object>();
            // execute outbound pipeline
            Pipeline.OutboundFinished += async (pipeline, bytes) =>
            {
                // finally, send bytes over the wire
                await _udpClient.SendAsync(bytes, bytes.Length, receiverEndPoint);
                tcs.SetResult(null);
            };
            Pipeline.Write(message);
            return tcs.Task;
        }

        public Task ReceiveAsync()
        {
            var t = _udpClient.ReceiveAsync();

            // execute inbound pipeline
            Pipeline.Read(bytes);
        }

        protected override void DoClose()
        {
            _udpClient.Close();
        }

        public override Socket Socket
        {
            get { return _udpClient.Client; }
        }

        public bool IsOpen
        {
            get { throw new NotImplementedException(); }
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
