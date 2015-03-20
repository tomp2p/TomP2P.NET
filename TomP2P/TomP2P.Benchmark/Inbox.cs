using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace TomP2P.Benchmark
{
    public class Inbox
    {
        // [bmArg] [repetitions] [resultsDir] [warmupSec] ([suffix])
        public static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.Error.WriteLine("Argument(s) missing.");
                Environment.Exit(-1);
            }
            var bmArg = args[0];
            var repetitions = Convert.ToInt32(args[1]);
            var resultsDir = args[2];
            var warmupSec = Convert.ToInt32(args[3]);
            var suffix = args.Length >= 5 ? args[4] : "";
            var arguments = new Arguments(bmArg, repetitions, resultsDir, warmupSec, suffix);

            try
            {
                if (repetitions < 1)
                {
                    throw new ArgumentException("Repetitions must be >= 1.");
                }
                ExecuteAsync(arguments).Wait();
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

        private static async Task ExecuteAsync(Arguments args)
        {
            Console.WriteLine("Argument: {0}", args.BmArg);
            Console.WriteLine("Repetitions: {0}", args.Repetitions);

            PrintStopwatchProperties();

            var results = new double[args.Repetitions];
            for (int i = 0; i < args.Repetitions; i++)
            {
                Console.WriteLine("Executing repetition {0} / {1}:", i + 1, args.Repetitions);
                double repetitionResult;
                switch (args.BmArg)
                {
                    case "bb1":
                        repetitionResult = await BootstrapBenchmark.Benchmark1Async(args);
                        break;
                    default:
                        throw new ArgumentException("No valid benchmark argument.");
                }

                // store repetition result
                results[i] = repetitionResult;
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
            Console.WriteLine("----------- RESULTS -----------");
            foreach (var res in results)
            {
                Console.WriteLine(res);
            }
            Console.WriteLine("Mean: {0} ms.", Statistics.CalculateMean(results));
            Console.WriteLine("Variance: {0} ms.", Statistics.CalculateVariance(results));
            Console.WriteLine("Standard Deviation: {0} ms.", Statistics.CalculateStdDev(results));
            Console.WriteLine("-------------------------------");
        }

        private static void WriteFile(Arguments args, double[] results)
        {
            var customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            //var location = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/benchmarking/NET/runtimesNET.txt";
            var path = args.ResultsDir + "\\" + args.BmArg + "_net" + args.Suffix + ".txt";
            
            using (var file = new StreamWriter(path))
            {
                file.WriteLine("{0}, {1}", "Repetition", "NET" + args.Suffix);
                for (int i = 0; i < results.Length; i++)
                {
                    file.WriteLine("{0}, {1}", i, results[i].ToString(customCulture));
                }
            }
            Console.WriteLine("Results written to {0}.", path);
        }
    }
}