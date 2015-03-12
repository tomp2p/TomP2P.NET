using System;
using System.Threading.Tasks;
using NLog;
using TomP2P.Core.P2P;

namespace TomP2P.Benchmark
{
    public static class BootstrapBenchmark
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static async Task Benchmark1Async()
        {
            var rnd = new Random(42);
            Peer master = null;

            try
            {
                // setup
                var peers = BenchmarkUtil.CreateNodes(10, rnd, 7077, true);
                master = peers[0];

                // bootstrap all slaves to the master
                var tasks = new Task[peers.Length-1];
                for (int i = 1; i < peers.Length; i++)
                {
                    tasks[i-1] = peers[i].Bootstrap().SetPeerAddress(master.PeerAddress).StartAsync();
                }
                await Task.WhenAll(tasks);
                Logger.Info("Bootstrap environment set up with {0} peers.", peers.Length);
                
                // wait for peers to know each other
                const int delaySec = 10;
                Logger.Info("Waiting {0} seconds...", delaySec);
                await Task.Delay(delaySec*1000);

                // bootstrap a new peer, measure time
                var newPeer = BenchmarkUtil.CreateSlave(master, rnd, true);

                var watch = BenchmarkUtil.StartBenchmark();
                await newPeer.Bootstrap().SetPeerAddress(master.PeerAddress).StartAsync();
                BenchmarkUtil.StopBenchmark(watch);
            }
            finally
            {
                if (master != null)
                {
                    master.ShutdownAsync().Wait();
                }
            }
        }
    }
}
