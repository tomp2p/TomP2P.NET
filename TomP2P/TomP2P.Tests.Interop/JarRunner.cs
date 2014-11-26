using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Tests.Interop
{
    public class JarRunner
    {
        //public const string TmpDir = "C:/Users/Christian/Desktop/interop/";
        public const string TmpDir = "D:/Desktop/interop/";

        private const string JavaExecutable = "C:/Program Files/Java/jre7/bin/java.exe";
        //private const string JavaArgs = "-jar C:/Users/Christian/Desktop/interop/interop.jar";
        private const string JavaArgs = "-jar D:/Desktop/interop/interop.jar";

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

        public static byte[] RequestJavaBytes([CallerMemberName] string testArgument = "")
        {
            Run(testArgument);

            var inputPath = String.Format("{0}{1}-out.txt", TmpDir, testArgument);
            byte[] bytes = File.ReadAllBytes(inputPath);
            return bytes;
        }

        private static void Run(string testArgument)
        {
            string jarArgs = String.Format("{0} {1}", JavaArgs, testArgument);

            var processInfo = new ProcessStartInfo(JavaExecutable, jarArgs)
            {
                CreateNoWindow = true,
                UseShellExecute = false,

                // redirect output stream
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process {StartInfo = processInfo};
            process.Start();

            string t = process.StandardOutput.ReadToEnd();
            Trace.WriteLine(t);

            process.WaitForExit();
            process.Close();
        }
    }
}
