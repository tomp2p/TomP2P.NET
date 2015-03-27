using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace TomP2P.Benchmark
{
    public class Inbox
    {
        // [bmArg] [type] [nrWarmups] [nrRepetitions] [resultsDir] ([suffix])
        public static void Main(string[] args)
        {
            if (args.Length < 5)
            {
                Console.Error.WriteLine("Argument(s) missing.");
                Environment.Exit(-1);
            }
            var bmArg = args[0];
            var type = args[1];
            var nrWarmups = Convert.ToInt32(args[2]);
            var nrRepetitions = Convert.ToInt32(args[3]);
            var resultsDir = args[4];
            var suffix = args.Length >= 6 ? args[5] : "";
            var arguments = new Arguments(bmArg, type, nrWarmups, nrRepetitions, resultsDir, suffix);

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

            double[] results = null;
            switch (args.BmArg)
            {
                case "bootstrap":
                    switch (args.Type)
                    {
                        case "cpu":
                            results = await new BootstrapProfiler().ProfileCpuAsync(args);
                            break;
                        case "memory":
                            results = await new BootstrapProfiler().ProfileMemoryAsync(args);
                            break;
                    }
                    break;
                case "send-local-udp":
                    switch (args.Type)
                    {
                        case "cpu":
                            results = await new SendDirectLocalProfiler(true).ProfileCpuAsync(args);
                            break;
                        case "memory":
                            results = await new SendDirectLocalProfiler(true).ProfileMemoryAsync(args);
                            break;
                    }
                    break;
                case "send-local-tcp":
                    switch (args.Type)
                    {
                        case "cpu":
                            results = await new SendDirectLocalProfiler(false).ProfileCpuAsync(args);
                            break;
                        case "memory":
                            results = await new SendDirectLocalProfiler(false).ProfileMemoryAsync(args);
                            break;
                    }
                    break;
                default:
                    throw new ArgumentException("No valid benchmark argument.");
            }

            PrintResults(results);
            WriteFile(args, results);
        }

        private static void PrintResults(double[] results)
        {
            Console.WriteLine("-------------------- RESULTS --------------------");
            foreach (var res in results)
            {
                Console.WriteLine(res);
            }
            Console.WriteLine("Mean: {0}", Statistics.CalculateMean(results));
            Console.WriteLine("Variance: {0}", Statistics.CalculateVariance(results));
            Console.WriteLine("Standard Deviation: {0}", Statistics.CalculateStdDev(results));
            Console.WriteLine("-------------------------------------------------");
        }

        private static void WriteFile(Arguments args, double[] results)
        {
            var customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            var path = String.Format("{0}\\{1}-{2}_net{3}.txt", args.ResultsDir, args.BmArg, args.Type, args.Suffix);

            using (var file = new StreamWriter(path))
            {
                file.WriteLine("{0}, {1}", "Iteration", "NET" + args.Type + args.Suffix);
                for (int i = 0; i < results.Length; i++)
                {
                    file.WriteLine("{0}, {1}", i, results[i].ToString(customCulture));
                }
            }
            Console.WriteLine("Results written to {0}.", path);
        }
    }
}