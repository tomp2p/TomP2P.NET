using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace TomP2P.Benchmark
{
    public class Inbox
    {
        // [bmArg] [nrWarmups] [nrRepetitions] [resultsDir] ([suffix])
        public static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.Error.WriteLine("Argument(s) missing.");
                Environment.Exit(-1);
            }
            var bmArg = args[0];
            var nrWarmups = Convert.ToInt32(args[1]);
            var nrRepetitions = Convert.ToInt32(args[2]);
            var resultsDir = args[3];
            var suffix = args.Length >= 5 ? args[4] : "";
            var arguments = new Arguments(bmArg, nrWarmups, nrRepetitions, resultsDir, suffix);

            try
            {
                if (nrRepetitions < 1)
                {
                    throw new ArgumentException("NrRepetitions must be >= 1.");
                }
                Console.WriteLine(arguments);
                ExecuteAsync(arguments).Wait();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception occurred:\n{0}.", ex);
                Console.WriteLine("Exiting due to error.");
                Environment.Exit(-2);
            }
            Console.WriteLine("Exiting with success.");
#if DEBUG
            Console.ReadLine();
#endif
            Environment.Exit(0);
        }

        private static async Task ExecuteAsync(Arguments args)
        {
            Console.WriteLine("Argument: {0}", args.BmArg);
            PrintStopwatchProperties();

            double[] results;
            switch (args.BmArg)
            {
                case "bootstrap":
                    results = await new BootstrapBenchmark().BenchmarkAsync(args);
                    break;
                default:
                    throw new ArgumentException("No valid benchmark argument.");
            }

            PrintResults(results);
            WriteFile(args, results);
        }

        private static void PrintStopwatchProperties()
        {
            Console.WriteLine("Stopwatch.Frequency: {0} ticks/sec", Stopwatch.Frequency);
            Console.WriteLine("Accurate within {0} nanoseconds.", 1000000000L / Stopwatch.Frequency);
            Console.WriteLine("Stopwatch.IsHighResolution: {0}", Stopwatch.IsHighResolution);
        }

        private static void PrintResults(double[] results)
        {
            Console.WriteLine("-------------------- RESULTS --------------------");
            foreach (var res in results)
            {
                Console.WriteLine(res);
            }
            Console.WriteLine("Mean: {0} ms.", Statistics.CalculateMean(results));
            Console.WriteLine("Variance: {0} ms.", Statistics.CalculateVariance(results));
            Console.WriteLine("Standard Deviation: {0} ms.", Statistics.CalculateStdDev(results));
            Console.WriteLine("-------------------------------------------------");
        }

        private static void WriteFile(Arguments args, double[] results)
        {
            var customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            var path = args.ResultsDir + "\\" + args.BmArg + "_net" + args.Suffix + ".txt";

            using (var file = new StreamWriter(path))
            {
                file.WriteLine("{0}, {1}", "Iteration", "NET" + args.Suffix);
                for (int i = 0; i < results.Length; i++)
                {
                    file.WriteLine("{0}, {1}", i, results[i].ToString(customCulture));
                }
            }
            Console.WriteLine("Results written to {0}.", path);
        }
    }
}