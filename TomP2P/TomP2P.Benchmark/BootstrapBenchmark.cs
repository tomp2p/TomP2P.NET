using System.Threading.Tasks;
using TomP2P.Core.P2P;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public class BootstrapBenchmark : BaseBenchmark
    {
        public static async Task<double> Benchmark1Async(Arguments args)
        {
            // each run should create same IDs
            var rnd = new InteropRandom(42);
            Peer master = null;
            try
            {
                // setup
                var peers = SetupNetwork(rnd);
                await BootstrapAllAsync(args, peers);
                await NetworkWarmup(args);
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
                // setup
                var peers = SetupNetwork(rnd);
                await BootstrapAllAsync(args, peers);
                await NetworkWarmup(args);
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
                // setup
                var peers = SetupNetwork(rnd);
                await BootstrapAllAsync(args, peers);
                await NetworkWarmup(args);
                master = peers[0];

                // benchmark: 20x peer creation, bootstrap
                var watch = BenchmarkUtil.StartBenchmark(args.BmArg);

                var tasks = new Task[20];
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
    }
}
