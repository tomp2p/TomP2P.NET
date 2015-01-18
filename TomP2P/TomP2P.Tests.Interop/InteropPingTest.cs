using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Connection;
using TomP2P.Extensions;
using TomP2P.P2P;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.Tests.Interop
{
    [TestFixture]
    public class InteropPingTest
    {
        [Test]
        public async void TestPingJavaUdp()
        {
            Peer sender = null;
            ChannelCreator cc = null;

            try
            {
                // setup Java receiver and get it's PeerAddress
                var paBytes = JarRunner.RequestJavaBytes("TestPingJavaUdp-start");
                var paBytes2 = InteropUtil.ReadJavaBytes(paBytes);
                var receiverAddress = new PeerAddress(paBytes2);

                // setup .NET sender
                sender = new PeerBuilder(new Number160("0x9876")).
                    SetP2PId(55).
                    SetPorts(2424).
                    Start();

                cc = await sender.ConnectionBean.Reservation.CreateAsync(1, 0);
                var handshake = new PingRpc(sender.PeerBean, sender.ConnectionBean);
                var task = handshake.PingUdpAsync(receiverAddress, cc, new DefaultConnectionConfiguration());
                await task;

                Assert.IsTrue(task.IsCompleted && !task.IsFaulted);
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
                // TODO shutdown Java receiver
            }
        }
    }
}
