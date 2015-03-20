using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Core.P2P;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public abstract class BaseBenchmark
    {
        protected static Peer[] SetupNetwork(InteropRandom rnd)
        {
            // setup peers
            return BenchmarkUtil.CreateNodes(500, rnd, 7077, true, false);
        }

        protected static async Task BootstrapAllAsync(Arguments args, IList<Peer> peers)
        {
            // bootstrap all slaves to the master
            var tasks = new Task[peers.Count - 1];
            for (int i = 1; i < peers.Count; i++)
            {
                //Logger.Info("Bootstraping slave {0} {1}.", i, peers[i]);
                tasks[i - 1] = peers[i].Bootstrap().SetPeerAddress(peers[0].PeerAddress).StartAsync();
            }
            Console.WriteLine("Waiting for all peers to finish bootstrap...");
            await Task.WhenAll(tasks);
            Console.WriteLine("Bootstrap environment set up with {0} peers.", peers.Count);
        }

        protected static async Task NetworkWarmup(Arguments args)
        {
            // wait for peers to know each other
            Console.WriteLine("Waiting {0} seconds...", args.WarmupSec);
            await Task.Delay(args.WarmupSec * 1000);
        }
    }
}
