using System.Threading.Tasks;
using TomP2P.Core.P2P.Builder;
using TomP2P.Core.Peers;

namespace TomP2P.Benchmark
{
    public class SendDirectRemoteProfiler : SendDirectProfiler
    {
        private const int NetworkSize = 1;
        private PeerAddress _remoteAddress;

        public SendDirectRemoteProfiler(bool isForceUdp)
            : base(isForceUdp)
        { }

        protected override async Task SetupAsync(Arguments args)
        {
            Network = BenchmarkUtil.CreateNodes(NetworkSize, Rnd, 7077, false, false);
            Sender = Network[0];
            _remoteAddress = args.Param as PeerAddress;

            Cc = await Sender.ConnectionBean.Reservation.CreateAsync(IsForceUdp ? 1 : 0, IsForceUdp ? 0 : 1);

            SendDirectBuilder = new SendDirectBuilder(Sender, (PeerAddress)null)
                .SetIdleUdpSeconds(0)
                .SetIdleTcpSeconds(0)
                .SetBuffer(CreateSampleBuffer())
                .SetIsForceUdp(IsForceUdp);
        }

        protected override async Task ExecuteAsync()
        {
            await Sender.DirectDataRpc.SendAsync(_remoteAddress, SendDirectBuilder, Cc);

            // make buffer reusable
            SendDirectBuilder.Buffer.Reset();
            SendDirectBuilder.Buffer.BackingBuffer.SetReaderIndex(0);
        }
    }
}
