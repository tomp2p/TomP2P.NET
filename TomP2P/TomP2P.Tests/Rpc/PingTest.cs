using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Connection;
using TomP2P.P2P;

namespace TomP2P.Tests.Rpc
{
    [TestFixture]
    public class PingTest
    {
        [Test]
        public void TestPingUdp()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;

            try
            {
                /*
                 * sender = new PeerBuilder(new Number160("0x9876")).p2pId(55).ports(2424).start();
            PingRPC handshake = new PingRPC(sender.peerBean(), sender.connectionBean());
            recv1 = new PeerBuilder(new Number160("0x1234")).p2pId(55).ports(8088).start();
            new PingRPC(recv1.peerBean(), recv1.connectionBean());
            FutureChannelCreator fcc = recv1.connectionBean().reservation().create(1, 0);
            fcc.awaitUninterruptibly();
            cc = fcc.channelCreator();
            FutureResponse fr = handshake.pingUDP(recv1.peerAddress(), cc,
                    new DefaultConnectionConfiguration());
            fr.awaitUninterruptibly();
            Assert.assertEquals(true, fr.isSuccess());
                 * */
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
