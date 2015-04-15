using System.Collections.Generic;
using System.Threading.Tasks;

namespace TomP2P.Benchmark
{
    public class BootstrapProfiler : Profiler
    {
        private const int NetworkSize = 5;
        private readonly IList<Task> _tasks = new List<Task>(NetworkSize * NetworkSize);

        protected override async Task SetupAsync(Arguments args)
        {
            Network = BenchmarkUtil.CreateNodes(NetworkSize, Rnd, 7077, false, false);
        }

        protected override async Task ShutdownAsync()
        {
            if (Network != null && Network[0] != null)
            {
                await Network[0].ShutdownAsync();
            }
        }

        protected override async Task ExecuteAsync()
        {
            for (int i = 0; i < Network.Length; i++)
            {
                for (int j = 0; j < Network.Length; j++)
                {
                    _tasks.Add(Network[i].Bootstrap().SetPeerAddress(Network[j].PeerAddress).StartAsync());
                }
            }
            await Task.WhenAll(_tasks);
        }
    }
}
