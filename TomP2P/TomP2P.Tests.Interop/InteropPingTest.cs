using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
        private TaskCompletionSource<PeerAddress> tcs;

        [Test]
        public async void TestPingJavaUdp()
        {
            // setup Java server and get it's PeerAddress
            tcs = new TaskCompletionSource<PeerAddress>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                JarRunner.RequestJavaBytes("TestPingJavaUdp-start", DataReceived);
            });
            PeerAddress server = await tcs.Task;

            // ping & test
            Peer sender = null;
            ChannelCreator cc = null;
            try
            {
                // setup .NET sender
                sender = new PeerBuilder(new Number160("0x9876")).
                    SetP2PId(55).
                    SetPorts(2424).
                    Start();

                cc = await sender.ConnectionBean.Reservation.CreateAsync(1, 0);
                var handshake = new PingRpc(sender.PeerBean, sender.ConnectionBean);
                var task = handshake.PingUdpAsync(server, cc, new DefaultConnectionConfiguration());
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
                JarRunner.WriteToProcess("TestPingJavaUdp-stop");
            }
        }

        private void DataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            // parse for the server PeerAddress, for this, it's required to 
            // know how it will be printed to the Java log

            // use a TCS to set the found address -> the calling method should await this result before proceeding

        }
    }
}
