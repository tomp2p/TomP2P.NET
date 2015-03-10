using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Connection;
using TomP2P.Extensions.Netty.Buffer;
using TomP2P.Extensions.Workaround;
using TomP2P.Message;
using TomP2P.P2P;
using TomP2P.P2P.Builder;
using TomP2P.Peers;
using TomP2P.Rpc;
using Buffer = TomP2P.Message.Buffer;

namespace TomP2P.Tests.Rpc
{
    [TestFixture]
    public class DirectTest
    {
        private static readonly VolatileInteger ReplyComplete = new VolatileInteger(0);
        private static readonly VolatileInteger ReplyNotComplete = new VolatileInteger(0);
        private static readonly VolatileInteger ProgressComplete = new VolatileInteger(0);
        private static readonly VolatileInteger ProgressNotComplete = new VolatileInteger(0);
        
        /*[Test]
        public void TestDirectMessage()
        {
            TestDirectMessage(true);
            TestDirectMessage(false);
        }*/

        [Test]
        public async void TestDirectMessage()
        {
            bool wait = true;

            Peer sender = null;
            Peer recv1 = null;
            ChannelCreator cc = null;
            try
            {
                sender = new PeerBuilder(new Number160("0x50"))
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(2424, 2424))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(2424)
                    .Start();
                recv1 = new PeerBuilder(new Number160("0x20"))
                    .SetChannelServerConfiguration(Utils2.CreateInfiniteTimeoutChannelServerConfiguration(8088, 8088))
                    .SetMaintenanceTask(Utils2.CreateInfiniteIntervalMaintenanceTask())
                    .SetP2PId(55)
                    .SetPorts(8088)
                    .Start();
                recv1.RawDataReply(new TestRawDataReply());
                cc = await sender.ConnectionBean.Reservation.CreateAsync(0, 1);

                var sendDirectBuilder = new SendDirectBuilder(sender, (PeerAddress) null);
                sendDirectBuilder.SetIsStreaming();
                sendDirectBuilder.SetIdleTcpSeconds(Int32.MaxValue);

                var bytes = new sbyte[50];
                var b = new Buffer(Unpooled.CompositeBuffer(), 100);
                b.AddComponent(Unpooled.WrappedBuffer(bytes));
                if (!wait)
                {
                    var replyBuffer = Unpooled.Buffer(50);
                    replyBuffer.SetWriterIndex(50);
                    b.AddComponent(replyBuffer);
                }
                sendDirectBuilder.SetBuffer(b);

                var taskResponse = sender.DirectDataRpc.SendAsync(recv1.PeerAddress, sendDirectBuilder, cc);
                if (wait)
                {
                    Thread.Sleep(500);
                    var replyBuffer = Unpooled.Buffer(50);
                    replyBuffer.SetWriterIndex(50);
                    b.AddComponent(replyBuffer);
                }
                await taskResponse;

                if (wait)
                {
                    Assert.AreEqual(1, ProgressComplete.Get());
                    Assert.AreEqual(1, ProgressNotComplete.Get());
                    Assert.AreEqual(1, ReplyComplete.Get());
                    Assert.AreEqual(1, ReplyNotComplete.Get());
                }
                else
                {
                    Assert.AreEqual(1, ProgressComplete.Get());
                    Assert.AreEqual(1, ProgressNotComplete.Get());
                    Assert.AreEqual(2, ReplyComplete.Get());
                    Assert.AreEqual(0, ReplyNotComplete.Get());
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
                ProgressComplete.Set(0);
                ProgressNotComplete.Set(0);
                ReplyComplete.Set(0);
                ReplyNotComplete.Set(0);
            }
        }

        private class TestRawDataReply : IRawDataReply
        {
            public Buffer Reply(PeerAddress sender, Buffer requestBuffer, bool complete)
            {
                Console.WriteLine("Reply 2 ? " + complete);
                var replyBuffer = Unpooled.Buffer(50); // TODO works?
                replyBuffer.SetWriterIndex(50);
                if (complete)
                {
                    ReplyComplete.IncrementAndGet();
                    return new Buffer(replyBuffer, 100);
                }
                else
                {
                    ReplyNotComplete.IncrementAndGet();
                    return new Buffer(replyBuffer, 100);
                }
            }
        }
    }
}
