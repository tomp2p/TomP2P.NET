using System.Threading.Tasks;
using TomP2P.Core.P2P;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public class BootstrapBenchmark : BaseBenchmark
    {
        private static readonly InteropRandom Rnd = new InteropRandom(42);
        private Peer[] _network;

        protected override void Setup()
        {
            _network = SetupNetwork(Rnd);
        }

        protected async override Task ExecuteAsync()
        {
            for (int i = 0; i < _network.Length; i++)
            {
                for (int j = 0; j < _network.Length; j++)
                {
                    // TODO better if not awaited?
                    await _network[i].Bootstrap().SetPeerAddress(_network[j].PeerAddress).StartAsync();
                }
            }
        }
    }
}
