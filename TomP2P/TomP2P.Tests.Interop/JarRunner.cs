using System.Diagnostics;

namespace TomP2P.Tests.Interop
{
    public class JarRunner
    {
        public static void Run()
        {
            const string fileName = "C:/Program Files/Java/jre7/bin/java.exe";
            const string args = "-jar C:/Users/Christian/Desktop/interop/interop.jar";

            var processInfo = new ProcessStartInfo(fileName, args)
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
