using System.Threading.Tasks;
using TomP2P.Core.P2P.Builder;
using TomP2P.Core.Peers;

namespace TomP2P.Benchmark
{
    public class SendDirectLocalProfiler : SendDirectProfiler
    {
        private const int NetworkSize = 2;

        public SendDirectLocalProfiler(bool isForceUdp)
            : base(isForceUdp)
        { }

        protected override async Task SetupAsync(Arguments args)
        {
            Network = BenchmarkUtil.CreateNodes(NetworkSize, Rnd, 7077, false, false);
            Sender = Network[0];
            Receiver = Network[1];
            Receiver.RawDataReply(new SampleRawDataReply());
            Cc = await Sender.ConnectionBean.Reservation.CreateAsync(IsForceUdp ? 1 : 0, IsForceUdp ? 0 : 1);

            SendDirectBuilder = new SendDirectBuilder(Sender, (PeerAddress)null)
                .SetIdleUdpSeconds(0)
                .SetIdleTcpSeconds(0)
                .SetBuffer(CreateSampleBuffer())
                .SetIsForceUdp(IsForceUdp);
        }

        protected override async Task ExecuteAsync()
        {
            await Sender.DirectDataRpc.SendAsync(Receiver.PeerAddress, SendDirectBuilder, Cc);

            // make buffer reusable
            SendDirectBuilder.Buffer.Reset();
            SendDirectBuilder.Buffer.BackingBuffer.SetReaderIndex(0);
        }
    }
}
