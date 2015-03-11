using System;
using NUnit.Framework;
using TomP2P.Connection;
using TomP2P.P2P;
using TomP2P.P2P.Builder;
using TomP2P.Peers;
using TomP2P.Rpc;
using TomP2P.Storage;
using Buffer = TomP2P.Message.Buffer;

namespace TomP2P.Tests.Rpc
{
    [TestFixture]
    public class DirectTest
    {
        private static readonly sbyte[] TestRawBytes = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        [Test]
        public async void TestDirect1()
        {
            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x50"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(2525, 2525))
                    .SetP2PId(55)
                    .SetPorts(2525)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x20"))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(9099, 9099))
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();
                recv1.RawDataReply(new TestRawDataReply());

                cc = await sender.ConnectionBean.Reservation.CreateAsync(0, 2);

                var sendDirectBuilder = new SendDirectBuilder(sender, (PeerAddress) null);
                sendDirectBuilder.SetIsStreaming();
                sendDirectBuilder.SetIdleTcpSeconds(Int32.MaxValue);
                var buffer = CreateTestBuffer();
                sendDirectBuilder.SetBuffer(buffer);

                var tr1 = sender.DirectDataRpc.SendAsync(recv1.PeerAddress, sendDirectBuilder, cc);
                //var tr2 = sender.DirectDataRpc.SendAsync(recv1.PeerAddress, sendDirectBuilder, cc);
                await tr1;
                //await tr2;
                Assert.IsTrue(!tr1.IsFaulted);
                //Assert.IsTrue(!tr2.IsFaulted);

                var ret = tr1.Result.Buffer(0);
                Assert.AreEqual(buffer, ret);
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

        private static Buffer CreateTestBuffer()
        {
            var acbb = AlternativeCompositeByteBuf.CompBuffer();
            acbb.WriteBytes(TestRawBytes);
            return new Buffer(acbb);
        }

        private class TestRawDataReply : IRawDataReply
        {
            public Buffer Reply(PeerAddress sender, Buffer requestBuffer, bool complete)
            {
                return CreateTestBuffer();
            }
        }
    }
}
