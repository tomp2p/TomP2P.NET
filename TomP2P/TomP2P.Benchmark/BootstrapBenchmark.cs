using System;
using System.Threading.Tasks;
using TomP2P.Core.P2P;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public static class BootstrapBenchmark
    {
        /*
         * Benchmark considerations:
         * - each run must generate the same IDs on bot platforms -> same routing
         * - use result, otherwise compiler might comment benchmarked statements
         * - do >3 repetitions
         * - JIT warmup
         * */

        public static async Task<double> Benchmark1Async(Arguments args)
        {
            // each run should create same IDs
            var rnd = new InteropRandom(42);
            Peer master = null;
            try
            {
                // setup
                var peers = BenchmarkUtil.CreateNodes(500, rnd, 7077, true, false);
                master = peers[0];

                // bootstrap all slaves to the master
                var tasks = new Task[peers.Length - 1];
                for (int i = 1; i < peers.Length; i++)
                {
                    //Logger.Info("Bootstraping slave {0} {1}.", i, peers[i]);
                    tasks[i - 1] = peers[i].Bootstrap().SetPeerAddress(master.PeerAddress).StartAsync();
                }
                Console.WriteLine("Waiting for all peers to finish bootstrap...");
                await Task.WhenAll(tasks);
                Console.WriteLine("Bootstrap environment set up with {0} peers.", peers.Length);

                // wait for peers to know each other
                Console.WriteLine("Waiting {0} seconds...", args.WarmupSec);
                await Task.Delay(args.WarmupSec * 1000);

                // bootstrap a new peer, measure time
                var newPeer = BenchmarkUtil.CreateSlave(master, rnd, true, false);

                var watch = BenchmarkUtil.StartBenchmark(args.BmArg);
                await newPeer.Bootstrap().SetPeerAddress(master.PeerAddress).StartAsync();
                return BenchmarkUtil.StopBenchmark(watch, args.BmArg);
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
