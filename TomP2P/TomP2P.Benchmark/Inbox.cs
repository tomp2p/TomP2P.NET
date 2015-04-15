using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TomP2P.Core.Peers;

namespace TomP2P.Benchmark
{
    public class Inbox
    {
        // [bmArg] [type] [nrWarmups] [nrRepetitions] [resultsDir] ([suffix])
        public static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "server")
            {
                Server.Setup();
            }
            else if (args.Length < 5)
            {
                Console.Error.WriteLine("Argument(s) missing.");
#if DEBUG
                Console.ReadLine();
#endif
                Environment.Exit(-1);
            }
            else
            {
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
#if DEBUG
                Console.ReadLine();
#endif
                    Environment.Exit(-2);
                }
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
                case "send-remote-udp":
                    DetermineServerAddress(args);
                    switch (args.Type)
                    {
                        case "cpu":
                            results = await new SendDirectRemoteProfiler(false).ProfileCpuAsync(args);
                            break;
                        case "memory":
                            results = await new SendDirectRemoteProfiler(false).ProfileMemoryAsync(args);
                            break;
                    }
                    break;
                // TODO send-remote-tcp
                default:
                    throw new ArgumentException("No valid benchmark argument.");
            }

            PrintResults(results);
            WriteFile(args, results);
        }

        /// <summary>
        /// Requests the server address from the user and stores it to the arguments.
        /// </summary>
        /// <param name="args"></param>
        private static void DetermineServerAddress(Arguments args)
        {
            // ask user for remote address
            Console.WriteLine("Please enter server address: [PeerID] [IP address] [TCP port] [UDP port]");
            var input = Console.ReadLine();
            if (input != null)
            {
                var parts = input.Split(' ');
                if (parts.Length != 4)
                {
                    throw new ArgumentException("PeerID, IP address, TCP and UDP ports required.");
                }
                var n160 = new Number160(parts[0]);
                var ip = IPAddress.Parse(parts[1]);
                var tcpPort = Int32.Parse(parts[2]);
                var udpPort = Int32.Parse(parts[3]);
                var serverAddress = new PeerAddress(n160, ip, tcpPort, udpPort);
                args.Param = serverAddress;
            }
            throw new NullReferenceException("input");
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
            var customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
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