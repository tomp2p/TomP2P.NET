using System;
using NUnit.Framework;
using TomP2P.Connection;
using TomP2P.Connection.Windows.Netty;
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

                cc = await sender.ConnectionBean.Reservation.CreateAsync(0, 1);

                var sendDirectBuilder = new SendDirectBuilder(sender, (PeerAddress) null);
                sendDirectBuilder.SetIsStreaming();
                sendDirectBuilder.SetIdleTcpSeconds(Int32.MaxValue);
                var buffer = CreateTestBuffer();
                sendDirectBuilder.SetBuffer(buffer);

                var tr1 = sender.DirectDataRpc.SendAsync(recv1.PeerAddress, sendDirectBuilder, cc);
                await tr1;
                Assert.IsTrue(!tr1.IsFaulted);

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

        [Test]
        public async void TestOrder()
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
                recv1.RawDataReply(new TestOrderRawDataReply());

                for (int i = 0; i < 500; i++)
                {
                    cc = await sender.ConnectionBean.Reservation.CreateAsync(0, 1);

                    var sendDirectBuilder = new SendDirectBuilder(sender, (PeerAddress) null);
                    var buffer = AlternativeCompositeByteBuf.CompBuffer().WriteInt(i);
                    sendDirectBuilder.SetBuffer(new Buffer(buffer));
                    sendDirectBuilder.SetIsStreaming();

                    var tr = sender.DirectDataRpc.SendAsync(recv1.PeerAddress, sendDirectBuilder, cc);
                    TomP2P.Utils.Utils.AddReleaseListener(cc, tr);
                    tr.ContinueWith(t =>
                    {
                        int j = t.Result.Buffer(0).BackingBuffer.ReadInt();
                        Console.WriteLine("Received {0}.", j);
                    });
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
                if (cc != null)
                {
                    cc.ShutdownAsync().Wait();
                }
            }
        }

        [Test]
        public async void TestDirectReconnect()
        {
            // TODO this test must be adapted to newest TomP2P implementation, where ports/configs are treated differently
            Peer sender = null;
            Peer recv1 = null;
            try
            {
                var ccohTcp = new CountConnectionOutboundHandler();
                var ccohUdp = new CountConnectionOutboundHandler();
                var filter = new TestPipelineFilter(ccohTcp, ccohUdp);
                var csc1 = PeerBuilder.CreateDefaultChannelServerConfiguration().SetPorts(new Ports(2424, 2424));
                var csc2 = PeerBuilder.CreateDefaultChannelServerConfiguration().SetPorts(new Ports(8088, 8088));
                var ccc = PeerBuilder.CreateDefaultChannelClientConfiguration();
                csc1.SetPipelineFilter(filter);
                csc2.SetPipelineFilter(filter);
                ccc.SetPipelineFilter(filter);

                sender = new PeerBuilder(new Number160("0x50"))
                    .SetP2PId(55)
                    .SetEnableMaintenanceRpc(false)
                    .SetChannelClientConfiguration(ccc)
                    .SetChannelServerConfiguration(csc1)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x20"))
                    .SetP2PId(55)
                    .SetEnableMaintenanceRpc(false)
                    .SetChannelClientConfiguration(ccc)
                    .SetChannelServerConfiguration(csc2)
                    .Start();

                recv1.RawDataReply(new TestRawDataReply());

                var peerConnection = sender.CreatePeerConnection(recv1.PeerAddress);
                ccohTcp.Reset();
                ccohUdp.Reset();

                var taskDirect1 = sender.SendDirect(peerConnection).SetBuffer(CreateTestBuffer())
                    .SetConnectionTimeoutTcpMillis(2000).SetIdleTcpSeconds(10*1000).Start().Task;
                await taskDirect1;
                // TODO await listeners?

                Assert.IsTrue(!taskDirect1.IsFaulted);
                Assert.AreEqual(2, ccohTcp.Total); //.NET: there are 2 csc's
                Assert.AreEqual(0, ccohUdp.Total);

                // TODO thread sleep?
                // send second with the same connection
                var taskDirect2 = sender.SendDirect(peerConnection).SetBuffer(CreateTestBuffer())
                    .Start().Task;
                await taskDirect2;

                Assert.IsTrue(!taskDirect2.IsFaulted);
                Assert.AreEqual(4, ccohTcp.Total); //.NET: 2 csc's, 2 server sessions
                Assert.AreEqual(0, ccohUdp.Total);

                await peerConnection.CloseAsync();
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

        private class TestOrderRawDataReply : IRawDataReply
        {
            public Buffer Reply(PeerAddress sender, Buffer requestBuffer, bool complete)
            {
                int i = requestBuffer.BackingBuffer.ReadInt();
                Console.WriteLine("Got {0}.", i);
                var buffer = AlternativeCompositeByteBuf.CompBuffer().WriteInt(i);
                return new Buffer(buffer);
            }
        }

        private class TestPipelineFilter : IPipelineFilter
        {
            private readonly CountConnectionOutboundHandler _ccohTcp;
            private readonly CountConnectionOutboundHandler _ccohUdp;

            public TestPipelineFilter(CountConnectionOutboundHandler ccohTcp, CountConnectionOutboundHandler ccohUdp)
            {
                _ccohTcp = ccohTcp;
                _ccohUdp = ccohUdp;
            }

            public Pipeline Filter(Pipeline pipeline, bool isTcp, bool isClient)
            {
                var filteredPipeline = new Pipeline();
                filteredPipeline.AddLast("counter", isTcp ? _ccohTcp : _ccohUdp);
                foreach (var hi in pipeline.HandlerItems)
                {
                    filteredPipeline.AddLast(hi.Name, hi.Handler);
                }
                return filteredPipeline;
            }
        }
    }
}
