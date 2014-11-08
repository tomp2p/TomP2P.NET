using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TomP2P.Tests.Interop
{
    public class JarRunner
    {
        // Note: avoid read/write deadlocks (see http://msdn.microsoft.com/en-us/library/system.diagnostics.process.standardoutput.aspx)

        public static void Run()
        {
            string fileName = "C:/Program Files/Java/jre7/bin/java.exe";
            //string jarPath = "C:/Users/Christian/Desktop/interop/interop.jar";
            string args = "-jar C:/Users/Christian/Desktop/interop/interop.jar";

            /*var processInfo = new ProcessStartInfo();
            processInfo.FileName = "C:/Program Files/Java/jre7/bin/java.exe"; // TODO .exe?
            processInfo.Arguments = String.Format("-jar {0}", jarPath);
            processInfo.CreateNoWindow = false;
            processInfo.UseShellExecute = false;

            var process = new Process();
            process.StartInfo = processInfo;

            // redirect output stream
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived +=ProcessOnOutputDataReceived;

            process.Start();

            //process.WaitForExit();
            process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            process.Close();*/

            var myProcess = new Process();
            var myProcessStartInfo = new ProcessStartInfo(fileName, args);

            myProcessStartInfo.UseShellExecute = false;
            myProcessStartInfo.RedirectStandardOutput = true;

            myProcess.StartInfo = myProcessStartInfo;
            myProcess.Start();

            string t = myProcess.StandardOutput.ReadToEnd();

            //<StreamReader myStreamReader = myProcess.StandardOutput;
            // Read the standard output of the spawned process. 
            //string myString = myStreamReader.ReadLine();
            Trace.WriteLine(t);

            //myProcess.WaitForExit();
            //myProcess.Close();
        }

    }
}
