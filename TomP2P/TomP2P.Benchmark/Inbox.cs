using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TomP2P.Benchmark
{
    public class Inbox
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Argument missing.");
                Environment.Exit(-1);
            }
            var argument = args[0];
            var repetitions = args.Length >= 2 ? Convert.ToInt32(args[1]) : 1;

            try
            {
                Console.WriteLine("Argument: {0}", argument);
                Console.WriteLine("Repetitions: {0}", repetitions);
                ExecuteAsync(argument, repetitions).Wait();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception occurred:\n{0}.", ex);
                Console.WriteLine("Exiting due to error.");
                Environment.Exit(-2);
            }
            Console.WriteLine("Exiting with success.");
            Console.ReadLine();
            Environment.Exit(0);
        }

        private static async Task ExecuteAsync(string argument, int repetitions)
        {
            PrintStopwatchProperties();
            for (int i = 0; i < repetitions; i++)
            {
                Console.WriteLine("Executing repetition {0} / {1}:", i+1, repetitions);
                switch (argument)
                {
                    case "bb1":
                        await BootstrapBenchmark.Benchmark1Async(i);
                        break;
                }
            }
        }

        private static void PrintStopwatchProperties()
        {
            Console.WriteLine("Stopwatch.Frequency: {0} ticks/sec", Stopwatch.Frequency);
            Console.WriteLine("Accurate within {0} nanoseconds.", 1000000000L / Stopwatch.Frequency);
            Console.WriteLine("Stopwatch.IsHighResolution: {0}", Stopwatch.IsHighResolution);
        }
    }
}
