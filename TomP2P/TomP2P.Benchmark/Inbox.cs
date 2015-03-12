using System;

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
                Execute(argument);
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

        private static void Execute(string argument)
        {
            
        }
    }
}
