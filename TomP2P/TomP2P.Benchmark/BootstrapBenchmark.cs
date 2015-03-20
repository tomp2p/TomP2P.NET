using System;
using System.Threading.Tasks;
using TomP2P.Core.P2P;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public static class BootstrapBenchmark
    {
        public static async Task<double> Benchmark1Async(Arguments args)
        {
            // each run should create same IDs
            var rnd = new InteropRandom(42);
            Peer master = null;
            try
            {
                var peers = await SetupNetwork(args, rnd);
                master = peers[0];

                // benchmark: bootstrap
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

        public static async Task<double> Benchmark2Async(Arguments args)
        {
            // each run should create same IDs
            var rnd = new InteropRandom(42);
            Peer master = null;
            try
            {
                var peers = await SetupNetwork(args, rnd);
                master = peers[0];

                // benchmark: peer creation, bootstrap
                var watch = BenchmarkUtil.StartBenchmark(args.BmArg);
                var newPeer = BenchmarkUtil.CreateSlave(master, rnd, true, false);
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

        public static async Task<double> Benchmark3Async(Arguments args)
        {
            // each run should create same IDs
            var rnd = new InteropRandom(42);
            Peer master = null;
            try
            {
                var peers = await SetupNetwork(args, rnd);
                master = peers[0];

                // benchmark: 10x peer creation, bootstrap
                var watch = BenchmarkUtil.StartBenchmark(args.BmArg);

                var tasks = new Task[10];
                for (int i = 0; i < tasks.Length; i++)
                {
                    var newPeer = BenchmarkUtil.CreateSlave(master, rnd, true, false);
                    tasks[i] = newPeer.Bootstrap().SetPeerAddress(master.PeerAddress).StartAsync();
                }
                await Task.WhenAll(tasks);
                
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

        private static async Task<Peer[]> SetupNetwork(Arguments args, InteropRandom rnd)
        {
            // setup peers
            var peers = BenchmarkUtil.CreateNodes(500, rnd, 7077, true, false);
            
            // bootstrap all slaves to the master
            var tasks = new Task[peers.Length - 1];
            for (int i = 1; i < peers.Length; i++)
            {
                //Logger.Info("Bootstraping slave {0} {1}.", i, peers[i]);
                tasks[i - 1] = peers[i].Bootstrap().SetPeerAddress(peers[0].PeerAddress).StartAsync();
            }
            Console.WriteLine("Waiting for all peers to finish bootstrap...");
            await Task.WhenAll(tasks);
            Console.WriteLine("Bootstrap environment set up with {0} peers.", peers.Length);

            // wait for peers to know each other
            Console.WriteLine("Waiting {0} seconds...", args.WarmupSec);
            await Task.Delay(args.WarmupSec * 1000);
            return peers;
        }
    }
}
