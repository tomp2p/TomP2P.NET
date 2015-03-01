using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.P2P;
using TomP2P.Peers;

namespace TomP2P.Tests.P2P
{
    [TestFixture]
    public class BootstrapTest
    {
        [Test]
        public async void TestBootstrapDiscover()
        {
            var rnd = new Random(42);
            Peer master = null;
            Peer slave = null;
            try
            {
                master = new PeerBuilder(new Number160(rnd))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(4001, 4001))
                    .SetPorts(4001)
                    .Start();
                slave = new PeerBuilder(new Number160(rnd))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(4002, 4002))
                    .SetPorts(4002)
                    .Start();

                var tcsDiscover = master.Discover().SetPeerAddress(slave.PeerAddress).Start();
                await tcsDiscover.Task;

                Assert.IsTrue(!tcsDiscover.Task.IsFaulted);
            }
            finally
            {
                if (master != null)
                {
                    master.ShutdownAsync().Wait();
                }
                if (slave != null)
                {
                    slave.ShutdownAsync().Wait();
                }
            }
        }

        [Test]
        public async void TestBootstrapFail()
        {
            var rnd = new Random(42);
            Peer master = null;
            Peer slave = null;
            try
            {
                master = new PeerBuilder(new Number160(rnd))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(4001, 4001))
                    .SetPorts(4001)
                    .Start();
                slave = new PeerBuilder(new Number160(rnd))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(4002, 4002))
                    .SetPorts(4002)
                    .Start();

                // bootstrap to another address
                var taskBootstrap = master.Bootstrap()
                    .SetInetAddress(IPAddress.Loopback)
                    .SetPorts(3000)
                    .StartAsync();
                await taskBootstrap;

                Assert.IsTrue(!taskBootstrap.IsFaulted);
            }
            finally
            {
                if (master != null)
                {
                    master.ShutdownAsync().Wait();
                }
                if (slave != null)
                {
                    slave.ShutdownAsync().Wait();
                }
            }
        }
    }
}
