using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
                Console.ReadLine();
                Environment.Exit(-1);
            }
            var argument = args[0];
            var repetitions = args.Length >= 2 ? Convert.ToInt32(args[1]) : 1;

            try
            {
                if (repetitions < 1)
                {
                    throw new ArgumentException("Repetitions must be >= 1.");
                }
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
            var results = new double[repetitions];
            for (int i = 0; i < repetitions; i++)
            {
                Console.WriteLine("Executing repetition {0} / {1}:", i+1, repetitions);
                double repetitionResult;
                switch (argument)
                {
                    case "bb1":
                        repetitionResult = await BootstrapBenchmark.Benchmark1Async();
                        break;
                    default:
                        throw new ArgumentException("No valid benchmark argument.");
                }

                // store repetition result
                results[i] = repetitionResult;
            }

            PrintResults(results);
            WriteFile(results);
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

        private static void WriteFile(double[] results)
        {
            var customCulture = (CultureInfo) System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            var location = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/benchmarking/NET/runtimesNET.txt";
            using (var file = new StreamWriter(location))
            {
                file.WriteLine("{0}, {1}", "Repetition", "RuntimeNET");
                for (int i = 0; i < results.Length; i++)
                {
                    file.WriteLine("{0}, {1}", i, results[i].ToString(customCulture));
                }
            }
            Console.WriteLine("Results written to {0}.", location);
        }
    }
}