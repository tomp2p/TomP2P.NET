using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TomP2P.Core.P2P;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public abstract class BaseBenchmark
    {
        public const int NetworkSize = 10;

        public async Task<double[]> BenchmarkAsync(Arguments args)
        {
            Console.WriteLine("Setting up...");
            Setup();

            var warmups = new double[args.NrWarmups];
            var repetitions = new double[args.NrRepetitions];

            BenchmarkUtil.WarmupTimer();
            BenchmarkUtil.ReclaimResources();
            Console.WriteLine("Started Benchmarking with {0} warmups, {1} repetitions...", warmups.Length, repetitions.Length);
            var watch = Stopwatch.StartNew();

            // warmups
            for (int i = 0; i < warmups.Length; i++)
            {
                Console.WriteLine("Warmup A {0}...", i);
                watch.Restart();
                await ExecuteAsync();
                warmups[i] = watch.ElapsedTicks;
            }
            
            // repetitions
            for (int i = 0; i < repetitions.Length; i++)
            {
                Console.WriteLine("Repetition {0}...", i);
                watch.Restart();
                await ExecuteAsync();
                repetitions[i] = watch.ElapsedTicks;
            }

            watch.Stop();
            Console.WriteLine("Stopped Benchmarking.");
            Console.WriteLine("{0:0.000} ns | {1:0.000} ms | {2:0.000} s", watch.ToNanos(), watch.ToMillis(), watch.ToSeconds());

            // combine warmup and benchmark results
            var results = new double[warmups.Length + repetitions.Length];
            Array.Copy(warmups, results, warmups.Length);
            Array.Copy(repetitions, 0, results, warmups.Length, repetitions.Length);
            
            // convert results from ticks to ms
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = (results[i] / Stopwatch.Frequency) * 1000;
            }
            return results;
        }

        protected abstract void Setup();

        protected abstract Task ExecuteAsync();

        protected static Peer[] SetupNetwork(InteropRandom rnd)
        {
            return BenchmarkUtil.CreateNodes(NetworkSize, rnd, 7077, false, false);
        }
    }
}
