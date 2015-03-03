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

namespace TomP2P.Tests.Rpc
{
    [TestFixture]
    public class NeighborTest
    {
        private const int PortTcp = 5001;
        private const int PortUdp = 5002;

        [Test]
        public async void TestNeighbor()
        {
            Peer sender = null;
            Peer recv1 = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x50"))
                .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(2424, 2424))
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();

                // add neighbors to the sender's peer map
                var addresses = Utils2.CreateDummyAddresses(300, PortTcp, PortUdp);
                for (int i = 0; i < addresses.Length; i++)
                {
                    sender.PeerBean.PeerMap.PeerFound(addresses[i], null, null);
                }

                // register neighbor RPC handlers
                var neighbors1 = new NeighborRpc(sender.PeerBean, sender.ConnectionBean); // TODO needed? registering?

                recv1 = new PeerBuilder(new Number160("0x20"))
                .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(8088, 8088))
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();
                var neighbors2 = new NeighborRpc(recv1.PeerBean, recv1.ConnectionBean);

                // ask sender for his neighbors
                var cc = await recv1.ConnectionBean.Reservation.CreateAsync(1, 0);
                var sv = new SearchValues(new Number160("0x1"), null);
                var infConfig = Utils2.CreateInfiniteConfiguration();
                var tr = neighbors2.CloseNeighborsAsync(sender.PeerAddress, sv, Message.Message.MessageType.Request2, cc,
                    infConfig);
                await tr;

                Assert.IsTrue(!tr.IsFaulted);

                // check if receiver got the neighbors
                var neighborSet = tr.Result.NeighborsSet(0);
                Assert.IsTrue(neighborSet.Size == 33); // TODO why?
                var neighbors = neighborSet.Neighbors.ToList();
                Assert.AreEqual(new Number160("0x1"), neighbors[0].PeerId);
                Assert.AreEqual(PortTcp, neighbors[1].TcpPort);
                Assert.AreEqual(PortUdp, neighbors[2].UdpPort);
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
