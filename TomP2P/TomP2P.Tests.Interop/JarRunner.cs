using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace TomP2P.Tests.Interop
{
    public class JarRunner
    {
        public const string TmpDir = "C:/Users/Christian/Desktop/interop/";

        public static void WriteBytesAndTestInterop( byte[] bytes, [CallerMemberName] string testName = "")
        {
            var path = String.Format("{0}{1}.txt", TmpDir, testName);
            File.WriteAllBytes(path, bytes);

            Run(testName);
        }

        private static void Run(string testArgument)
        {
            const string fileName = "C:/Program Files/Java/jre7/bin/java.exe";
            const string args = "-jar C:/Users/Christian/Desktop/interop/interop.jar";

            string jarArgs = String.Format("{0} {1}", args, testArgument);

            var processInfo = new ProcessStartInfo(fileName, jarArgs)
            {
                //CreateNoWindow = true;
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
