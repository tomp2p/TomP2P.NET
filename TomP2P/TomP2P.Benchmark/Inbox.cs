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

            try
            {
                Console.WriteLine("Argument: {0}", argument);
                ExecuteAsync(argument).Wait();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception occurred:\n{0}.", ex);
                Console.WriteLine("Exiting due to error.");
                Environment.Exit(-2);
            }
            Console.WriteLine("Exiting with success.");
            Environment.Exit(0);
        }

        private static async Task ExecuteAsync(string argument)
        {
            PrintStopwatchProperties();
            switch (argument)
            {
                case "bb1":
                    await BootstrapBenchmark.Benchmark1Async();
                    break;
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
