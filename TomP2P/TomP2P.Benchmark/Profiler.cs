using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TomP2P.Core.P2P;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public abstract class Profiler
    {
        protected static readonly InteropRandom Rnd = new InteropRandom(42);
        protected Peer[] Network;

        public async Task<double[]> ProfileCpuAsync(Arguments args)
        {
            try
            {
                BenchmarkUtil.PrintStopwatchProperties();

                Console.WriteLine("Setting up...");
                await SetupAsync();

                var warmups = new long[args.NrWarmups];
                var repetitions = new long[args.NrRepetitions];

                BenchmarkUtil.WarmupTimer();
                BenchmarkUtil.ReclaimResources();
                Console.WriteLine("Started CPU profiling with {0} warmups, {1} repetitions...", warmups.Length, repetitions.Length);
                var watch = Stopwatch.StartNew();

                // warmups
                for (int i = 0; i < warmups.Length; i++)
                {
                    Console.WriteLine("Warmup {0}...", i);
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
                Console.WriteLine("Stopped CPU profiling.");

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
            finally
            {
                Console.WriteLine("Shutting down...");
                ShutdownAsync().Wait();
            }
        }

        public async Task<double[]> ProfileMemoryAsync(Arguments args)
        {
            try
            {
                Console.WriteLine("Setting up...");
                await SetupAsync();

                var warmups = new long[args.NrWarmups];
                var repetitions = new long[args.NrRepetitions];

                BenchmarkUtil.ReclaimResources();
                Console.WriteLine("Started memory profiling with {0} warmups, {1} repetitions...", warmups.Length, repetitions.Length);

                // TODO combine memory/repetitions?
                // warmups
                for (int i = 0; i < warmups.Length; i++)
                {
                    Console.WriteLine("Warmup {0}...", i);
                    await ExecuteAsync();

                    // dispose process object directly after usage
                    using (var proc = Process.GetCurrentProcess())
                    {
                        warmups[i] = proc.PrivateMemorySize64;
                    }
                }

                // repetitions
                for (int i = 0; i < repetitions.Length; i++)
                {
                    Console.WriteLine("Repetition {0}...", i);
                    await ExecuteAsync();
                    // dispose process object directly after usage
                    using (var proc = Process.GetCurrentProcess())
                    {
                        repetitions[i] = proc.PrivateMemorySize64;
                    }
                }

                Console.WriteLine("Stopped memory profiling.");

                // combine warmup and benchmark results
                var results = new double[warmups.Length + repetitions.Length];
                Array.Copy(warmups, results, warmups.Length);
                Array.Copy(repetitions, 0, results, warmups.Length, repetitions.Length);

                // convert results from bytes to megabytes
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = results[i] / 1000000;
                }
                return results;
            }
            finally
            {
                Console.WriteLine("Shutting down...");
                ShutdownAsync().Wait();
            }
        }

        protected abstract Task SetupAsync();

        protected abstract Task ShutdownAsync();

        protected abstract Task ExecuteAsync();
    }
}
