using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Tests.Interop
{
    public class JarRunner
    {
        private static Process _process;

        public const string TmpDir = "C:/Users/Christian/Desktop/interop/";
        //public const string TmpDir = "D:/Desktop/interop/";

        private const string JavaExecutable = "C:/Program Files/Java/jre7/bin/java.exe";
        private const string JavaArgs = "-jar C:/Users/Christian/Desktop/interop/TomP2P.Interop.jar";
        //private const string JavaArgs = "-jar D:/Desktop/interop/interop.jar";

        public static bool WriteBytesAndTestInterop(byte[] bytes, [CallerMemberName] string testArgument = "")
        {
            var outputPath = String.Format("{0}{1}-in.txt", TmpDir, testArgument);
            var inputPath = String.Format("{0}{1}-out.txt", TmpDir, testArgument);

            File.WriteAllBytes(outputPath, bytes);

            Run(testArgument);

            byte[] result = File.ReadAllBytes(inputPath);

            var javaReader = new JavaBinaryReader(new MemoryStream(result));

            // 1: test succeeded
            // 0: test failed
            int res = javaReader.ReadByte();
            return res == 1;
        }

        public static byte[] RequestJavaBytes([CallerMemberName] string testArgument = "", DataReceivedEventHandler dataReceived = null)
        {
            Run(testArgument, dataReceived);
            return ReadJavaResult(testArgument);
        }

        public static byte[] ReadJavaResult([CallerMemberName] string testArgument = "")
        {
            var inputPath = String.Format("{0}{1}-out.txt", TmpDir, testArgument);
            byte[] bytes = File.ReadAllBytes(inputPath);
            return bytes;
        }

        public static void Run(string testArgument, DataReceivedEventHandler dataReceived = null)
        {
            string jarArgs = String.Format("{0} {1}", JavaArgs, testArgument);

            var processInfo = new ProcessStartInfo(JavaExecutable, jarArgs)
            {
                CreateNoWindow = true,
                UseShellExecute = false, // redirected streams

                // redirect output stream
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            _process = new Process { StartInfo = processInfo, EnableRaisingEvents = true };
            _process.OutputDataReceived += ProcessOnOutputDataReceived;
            _process.ErrorDataReceived += ProcessOnErrorDataReceived;
            if (dataReceived != null)
            {
                _process.OutputDataReceived += dataReceived;
                _process.ErrorDataReceived += dataReceived;
            }
            _process.Start();
            Trace.WriteLine("Java process started.");

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            Trace.WriteLine("Waiting for Java process to exit.");
            _process.WaitForExit();
            Trace.WriteLine("Java process exited.");
            _process.Close();
        }

        private static void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                Trace.TraceError("JAVA [ERROR]: " + args.Data);
            }
        }

        private static void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                Trace.WriteLine("JAVA: " + args.Data);
            }
        }

        public static void WriteToProcess(string arguments)
        {
            if (_process != null)
            {
                StreamWriter sw = _process.StandardInput;
                sw.WriteLine(arguments);
                sw.Close();
            }
        }
    }
}
