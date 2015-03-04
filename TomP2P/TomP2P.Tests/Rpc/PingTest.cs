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
        public async void TestPingTcp2()
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

                // TODO check release in cc for first ping
                cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, 1);
                var tr = sender.PingRpc.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                await tr;
                var tr2 = recv1.PingRpc.PingTcpAsync(sender.PeerAddress, cc, new DefaultConnectionConfiguration());
                await tr2;

                Assert.IsTrue(!tr.IsFaulted);
                Assert.IsTrue(!tr2.IsFaulted);
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

        [Test]
        public async void TestPingTcpDeadlock()
        {
            Peer sender1 = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            try
            {
                sender1 = new PeerBuilder(new Number160("0x9876")).
                    SetP2PId(55).
                    SetPorts(2424).
                    Start();
                recv1 = new PeerBuilder(new Number160("0x1234")).
                    SetP2PId(55).
                    SetPorts(8088).
                    Start();

                cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, 1);

                var tr = sender1.PingRpc.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                await tr;
                await tr.ContinueWith(async t =>
                {
                    var tr2 = sender1.PingRpc.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                    try
                    {
                        await tr2;
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail(ex.ToString());
                    }
                });
                Assert.IsTrue(!tr.IsFaulted);
            }
            finally
            {
                if (cc != null)
                {
                    cc.ShutdownAsync().Wait();
                }
                if (sender1 != null)
                {
                    sender1.ShutdownAsync().Wait();
                }
                if (recv1 != null)
                {
                    recv1.ShutdownAsync().Wait();
                }
            }
        }

        // TODO in Java: fix
        [Ignore]
        [Test]
        public async void TestPingHandlerError()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x1234"))
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();
                var handshake1 = new PingRpc(sender.PeerBean, sender.ConnectionBean, false, true, false);
                var handshake2 = new PingRpc(recv1.PeerBean, recv1.ConnectionBean, false, true, false);

                cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, 1);

                var tr = handshake1.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                await tr;

                Assert.IsTrue(tr.IsFaulted);
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
        public async void TestPingTimeoutTcp()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x1234"))
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();
                var handshake1 = new PingRpc(sender.PeerBean, sender.ConnectionBean, false, true, true);
                var handshake2 = new PingRpc(recv1.PeerBean, recv1.ConnectionBean, false, true, true);

                cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, 1);

                var tr = handshake1.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                await tr;

                Assert.IsTrue(tr.IsFaulted);
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
