using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Connection;
using TomP2P.P2P;
using TomP2P.Peers;

namespace TomP2P.Tests.Rpc
{
    [TestFixture]
    public class ReservationTest
    {
        [Test]
        public async void TestReservationTcp()
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
                cc = await recv1.ConnectionBean.Reservation.CreateAsync(0, 3);

                for (int i = 0; i < 100; i++)
                {
                    var tr1 = sender.PingRpc.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                    var tr2 = sender.PingRpc.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                    var tr3 = sender.PingRpc.PingTcpAsync(recv1.PeerAddress, cc, new DefaultConnectionConfiguration());
                    await tr1;
                    await tr2;
                    await tr3;
                    Assert.IsTrue(!tr1.IsFaulted);
                    Assert.IsTrue(!tr2.IsFaulted);
                    Assert.IsTrue(!tr3.IsFaulted);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
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
                if (cc != null)
                {
                    cc.ShutdownAsync().Wait();
                }
            }
        }
    }
}
