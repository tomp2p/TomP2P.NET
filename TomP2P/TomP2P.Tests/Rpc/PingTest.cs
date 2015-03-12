using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Core.Connection;
using TomP2P.Core.Message;
using TomP2P.Core.P2P;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

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
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x1234"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();
                var handshake1 = new PingRpc(sender.PeerBean, sender.ConnectionBean, false, true, true);
                var handshake2 = new PingRpc(recv1.PeerBean, recv1.ConnectionBean, false, true, true);

                cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, 1);

                var tr = handshake1.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());

                try
                {
                    await tr;
                    Assert.Fail("Timeout should have let task fail.");
                }
                catch (Exception)
                {
                    Assert.IsTrue(tr.IsFaulted);
                }
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
        public async void TestPingTimeoutUdp()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x1234"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();
                var handshake1 = new PingRpc(sender.PeerBean, sender.ConnectionBean, false, true, true);
                var handshake2 = new PingRpc(recv1.PeerBean, recv1.ConnectionBean, false, true, true);

                cc = await recv1.ConnectionBean.Reservation.CreateAsync(1, 0);

                var tr = handshake1.PingUdpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());

                try
                {
                    await tr;
                    Assert.Fail("Timeout should have let task fail.");
                }
                catch (Exception)
                {
                    Assert.IsTrue(tr.IsFaulted);
                }
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

        // TODO TestPingHandlerFailure

        [Test]
        public async void TestPingTcpPool()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x1234"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();

                var tasks = new List<Task<Message>>(50);
                cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, tasks.Capacity);
                for (int i = 0; i < tasks.Capacity; i++)
                {
                    var taskResponse = sender.PingRpc.PingTcpAsync(recv1.PeerAddress, cc,
                        new DefaultConnectionConfiguration());
                    tasks.Add(taskResponse);
                }
                foreach (var task in tasks)
                {
                    await task;
                    Assert.IsTrue(!task.IsFaulted);
                }
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
        public async void TestPingTcpPool2()
        {
            var peers = new Peer[50];
            try
            {
                for (int i = 0; i < peers.Length; i++)
                {
                    peers[i] = new PeerBuilder(Number160.CreateHash(i))
                        .SetP2PId(55)
                        .SetPorts(2424 + i)
                        .Start();
                }
                var tasks = new List<Task<Message>>(50);
                for (int i = 0; i < peers.Length; i++)
                {
                    var cc = await peers[0].ConnectionBean.Reservation.CreateAsync(0, 1);

                    var taskResponse = peers[0].PingRpc.PingTcpAsync(peers[i].PeerAddress, cc,
                        new DefaultConnectionConfiguration());
                    Core.Utils.Utils.AddReleaseListener(cc, taskResponse);
                    tasks.Add(taskResponse);
                }
                foreach (var task in tasks)
                {
                    await task;
                    Assert.IsTrue(!task.IsFaulted);
                }
            }
            finally
            {
                for (int i = 0; i < peers.Length; i++)
                {
                    peers[i].ShutdownAsync().Wait();
                }
            }
        }

        [Test]
        public async void TestPingUdpPool()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x1234"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();

                var tasks = new List<Task<Message>>(50);
                cc = await recv1.ConnectionBean.Reservation.CreateAsync(tasks.Capacity, 0);
                for (int i = 0; i < tasks.Capacity; i++)
                {
                    var taskResponse = sender.PingRpc.PingUdpAsync(recv1.PeerAddress, cc,
                        new DefaultConnectionConfiguration());
                    tasks.Add(taskResponse);
                }
                foreach (var task in tasks)
                {
                    await task;
                    Assert.IsTrue(!task.IsFaulted);
                }
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
        public async void TestPingUdpPool2()
        {
            var peers = new Peer[50];
            try
            {
                for (int i = 0; i < peers.Length; i++)
                {
                    peers[i] = new PeerBuilder(Number160.CreateHash(i))
                        .SetP2PId(55)
                        .SetPorts(2424 + i)
                        .Start();
                }
                var tasks = new List<Task<Message>>(50);
                for (int i = 0; i < peers.Length; i++)
                {
                    var cc = await peers[0].ConnectionBean.Reservation.CreateAsync(1, 0);

                    var taskResponse = peers[0].PingRpc.PingUdpAsync(peers[i].PeerAddress, cc,
                        new DefaultConnectionConfiguration());
                    Core.Utils.Utils.AddReleaseListener(cc, taskResponse);
                    tasks.Add(taskResponse);
                }
                foreach (var task in tasks)
                {
                    await task;
                    Assert.IsTrue(!task.IsFaulted);
                }
            }
            finally
            {
                for (int i = 0; i < peers.Length; i++)
                {
                    peers[i].ShutdownAsync().Wait();
                }
            }
        }

        [Test]
        public async void TestPingTimeTcp()
        {
            Peer sender = null;
            Peer recv1 = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x9876"))
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();

                long start = Convenient.CurrentTimeMillis();
                var tasks = new List<Task<Message>>(1000);
                for (int i = 0; i < 20; i++)
                {
                    var cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, 50);
                    for (int j = 0; j < 50; j++)
                    {
                        var taskResponse = sender.PingRpc.PingTcpAsync(recv1.PeerAddress, cc,
                            new DefaultConnectionConfiguration());
                        tasks.Add(taskResponse);
                    }
                    foreach (var task in tasks)
                    {
                        await task;
                        Assert.IsTrue(!task.IsFaulted);
                    }
                    tasks.Clear();
                    await cc.ShutdownAsync();
                }
                Console.WriteLine("TCP time: {0}ms.", Convenient.CurrentTimeMillis() - start);
            }
            finally
            {
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
        public async void TestPingTimeUdp()
        {
            Peer sender = null;
            Peer recv1 = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x9876"))
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();

                long start = Convenient.CurrentTimeMillis();
                var tasks = new List<Task<Message>>(1000);
                for (int i = 0; i < 20; i++)
                {
                    var cc = await recv1.ConnectionBean.Reservation.CreateAsync(50, 0);
                    for (int j = 0; j < 50; j++)
                    {
                        var taskResponse = sender.PingRpc.PingUdpAsync(recv1.PeerAddress, cc,
                            new DefaultConnectionConfiguration());
                        tasks.Add(taskResponse);
                    }
                    foreach (var task in tasks)
                    {
                        await task;
                        Assert.IsTrue(!task.IsFaulted);
                    }
                    tasks.Clear();
                    await cc.ShutdownAsync();
                }
                Console.WriteLine("UDP time: {0}ms.", Convenient.CurrentTimeMillis() - start);
            }
            finally
            {
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
        public void TestPingReserverLoop()
        {
            for (int i = 0; i < 100; i++)
            {
                TestPingReserve();
            }
        }

        [Test]
        public async void TestPingReserve()
        {
            Peer sender = null;
            Peer recv1 = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x9876"))
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x9876"))
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();
                var cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, 1);

                var taskResponse1 = sender.PingRpc.PingTcpAsync(recv1.PeerAddress, cc,
                    new DefaultConnectionConfiguration());
                var listenerCompl = Core.Utils.Utils.AddReleaseListener(cc, taskResponse1);
                await taskResponse1;
                await listenerCompl; // await release of reservations
                Assert.IsTrue(!taskResponse1.IsFaulted);

                var taskResponse2 = sender.PingRpc.PingTcpAsync(recv1.PeerAddress, cc,
                    new DefaultConnectionConfiguration());
                try
                {
                    await taskResponse2;
                    Assert.Fail("The already shut down reservation should have prohibited a new channel creation.");
                }
                catch (TaskFailedException)
                {
                    // reservations have been released already
                    Assert.IsTrue(taskResponse2.IsFaulted);
                }
            }
            finally
            {
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
