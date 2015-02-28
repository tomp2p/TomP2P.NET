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
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(2424, 2424))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x1234"))
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(7777, 7777))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(7777)
                    .Start();

                // bootstrap first
                await sender.Bootstrap().SetPeerAddress(recv1.PeerAddress).StartAsync();

                Assert.IsTrue(sender.PeerBean.PeerMap.All.Count == 1);
                Assert.IsTrue(recv1.PeerBean.PeerMap.AllOverflow.Count == 1);

                // graceful shutdown
                cc = await recv1.ConnectionBean.Reservation.CreateAsync(1, 0);

                var shutdownBuilder = new ShutdownBuilder(sender);

                await sender.QuitRpc.QuitAsync(recv1.PeerAddress, shutdownBuilder, cc);
                await sender.ShutdownAsync();
                sender = null; // ignore finally-block shutdown

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
