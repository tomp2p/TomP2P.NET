using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace TomP2P.Tests
{
    [TestFixture]
    public class LoggerTest
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Test]
        public void TestLogger()
        {
            Logger.Trace("This is a TRACE log entry.");
            Logger.Debug("This is a DEBUG log entry.");
            Logger.Info("This is an INFO log entry.");
            Logger.Warn("This is a WARN log entry.");
            Logger.Error("This is an ERROR log entry.");
            Logger.Fatal("This is a FATAL log entry.");
        }

    }
}
