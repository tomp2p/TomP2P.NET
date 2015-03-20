using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Core.P2P;
using TomP2P.Core.Peers;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public class PeerCreationBenchmark : BaseBenchmark
    {
        public static double Benchmark1(Arguments args)
        {
            // each run should create same IDs
            var rnd = new InteropRandom(42);
            // JIT warmup
            int anker = 0;
            for (int i = 0; i < 500; i++)
            {
                anker |= BenchmarkUtil.CreateNodes(1, rnd, 7077, true, true).Length;
            }

            // benchmark: peer creation
            var watch = BenchmarkUtil.StartBenchmark(args.BmArg);

            anker |= BenchmarkUtil.CreateNodes(1, rnd, 7077, true, true).Length;

            var res = BenchmarkUtil.StopBenchmark(watch, args.BmArg);
            BenchmarkUtil.AnkerTrash(anker);
            return res;
        }
    }
}
