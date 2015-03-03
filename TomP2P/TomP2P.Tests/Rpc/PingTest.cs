using System;
using NUnit.Framework;
using TomP2P.Connection;
using TomP2P.P2P;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.Tests.Rpc
{
    [TestFixture]
    public class PingTest
    {
        [Test]
        public async void TestPingTcp()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(2424, 2424))
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x1234"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(8088, 8088))
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();

                cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, 1);
                var tr = sender.PingRpc.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                await tr;

                Assert.IsTrue(!tr.IsFaulted);
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



        [Test]
        public async void TestPingUdp()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;

            try
            {
                sender = new PeerBuilder(new Number160("0x9876")).
                    SetP2PId(55).
                    SetPorts(2424).
                    Start();
                recv1 = new PeerBuilder(new Number160("0x1234")).
                    SetP2PId(55).
                    SetPorts(8088).
                    Start();
                var handshake = new PingRpc(sender.PeerBean, sender.ConnectionBean);

                cc = await recv1.ConnectionBean.Reservation.CreateAsync(1, 0);

                var t = handshake.PingUdpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                await t;
                Assert.IsTrue(t.IsCompleted && !t.IsFaulted);
            }
            catch (Exception ex)
            {
                throw ex;
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
