using System;
using System.Threading.Tasks;
using NLog;
using TomP2P.Core.P2P;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public static class BootstrapBenchmark
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /*
         * Benchmark considerations:
         * - each run must generate the same IDs on bot platforms -> same routing
         * - use result, otherwise compiler might comment benchmarked statements
         * - do >3 repetitions
         * - JIT warmup
         * */

        public static async Task<double> Benchmark1Async(int repetitionNr)
        {
            // each run should create same IDs
            var rnd = new InteropRandom(42);
            Peer master = null;
            try
            {
                // setup
                var peers = BenchmarkUtil.CreateNodes(500, rnd, 7077, false, false);
                master = peers[0];

                // bootstrap all slaves to the master
                var tasks = new Task[peers.Length - 1];
                for (int i = 1; i < peers.Length; i++)
                {
                    //Logger.Info("Bootstraping slave {0} {1}.", i, peers[i]);
                    tasks[i - 1] = peers[i].Bootstrap().SetPeerAddress(master.PeerAddress).StartAsync();
                }
                Logger.Info("Waiting for all peers to finish bootstrap...");
                await Task.WhenAll(tasks);
                Logger.Info("Bootstrap environment set up with {0} peers.", peers.Length);

                // wait for peers to know each other
                const int delaySec = 10;
                Logger.Info("Waiting {0} seconds...", delaySec);
                await Task.Delay(delaySec * 1000);

                // bootstrap a new peer, measure time
                var newPeer = BenchmarkUtil.CreateSlave(master, rnd, true, false);

                var watch = BenchmarkUtil.StartBenchmark();
                await newPeer.Bootstrap().SetPeerAddress(master.PeerAddress).StartAsync();
                return BenchmarkUtil.StopBenchmark(watch);
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
