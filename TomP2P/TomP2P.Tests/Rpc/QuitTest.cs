using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Connection;
using TomP2P.P2P;
using TomP2P.P2P.Builder;
using TomP2P.Peers;

namespace TomP2P.Tests.Rpc
{
    [TestFixture]
    public class QuitTest
    {
        [Test]
        public async void TestGracefulHalt()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            var infiniteTimeoutConfig = Utils2.CreateInfiniteTimeoutChannelServerConfiguration());
            var infiniteMaintenanceTask = Utils2.CreateInfiniteIntervalMaintenanceTask();
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetChannelServerConfiguration(infiniteTimeoutConfig)
                    .SetMaintenanceTask(infiniteMaintenanceTask)
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x1234"))
                    .SetChannelServerConfiguration(infiniteTimeoutConfig)
                    .SetMaintenanceTask(infiniteMaintenanceTask)
                    .SetP2PId(55)
                    .SetPorts(7777)
                    .Start();

                await sender.Bootstrap().SetPeerAddress(recv1.PeerAddress).StartAsync();

                Assert.IsTrue(sender.PeerBean.PeerMap.All.Count == 1);
                Assert.IsTrue(sender.PeerBean.PeerMap.AllOverflow.Count == 1);

                // graceful shutdown
                cc = await recv1.ConnectionBean.Reservation.CreateAsync(1, 0);

                var shutdownBuilder = new ShutdownBuilder(sender);

                await sender.QuitRpc.QuitAsync(recv1.PeerAddress, shutdownBuilder, cc);
                await sender.ShutdownAsync();

                Assert.IsTrue(recv1.PeerBean.PeerMap.All.Count == 0);
            }
            finally
            {
                if (cc != null)
                {
                    cc.ShutdownAsync().Wait();
                }
                if (sender != null)
                {
                    sender.ShutdownAsync().Wait();
                }
                if (recv1 != null)
                {
                    recv1.ShutdownAsync().Wait();
                }
            }
        }
    }
}
