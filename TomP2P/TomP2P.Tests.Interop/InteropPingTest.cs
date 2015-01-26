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
        private TaskCompletionSource<PeerAddress> _tcs;

        [Test]
        public async void TestPingJavaUdp()
        {
            // setup Java server and get it's PeerAddress
            _tcs = new TaskCompletionSource<PeerAddress>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                JarRunner.Run("TestPingJavaUdp-start", DataReceived);
            });
            PeerAddress server = await _tcs.Task;

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
                var responseMessage = await task;

                Assert.IsTrue(task.IsCompleted && !task.IsFaulted);
                Assert.AreEqual(responseMessage.Sender, server);
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
                JarRunner.WriteToProcess("TestPingJavaUdp-stop");
            }
        }

        [Test]
        public async void TestPingJavaBroadcastUdp()
        {
            // setup Java server and get it's PeerAddress
            _tcs = new TaskCompletionSource<PeerAddress>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                JarRunner.Run("TestPingJavaUdp-start", DataReceived);
            });
            PeerAddress server = await _tcs.Task;

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
                var task = handshake.PingBroadcastUdpAsync(server, cc, new DefaultConnectionConfiguration());
                var responseMessage = await task;

                Assert.IsTrue(task.IsCompleted && !task.IsFaulted);
                Assert.AreEqual(responseMessage.Sender, server);
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
                JarRunner.WriteToProcess("TestPingJavaUdp-stop");
            }
        }

        [Test]
        public async void TestPingJavaFireUdp()
        {
            // setup Java server and get it's PeerAddress
            _tcs = new TaskCompletionSource<PeerAddress>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                JarRunner.Run("TestPingJavaUdp-start", DataReceived);
            });
            PeerAddress server = await _tcs.Task;

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
                var task = handshake.FireUdpAsync(server, cc, new DefaultConnectionConfiguration());
                var responseMessage = await task;

                Assert.IsTrue(task.IsCompleted && !task.IsFaulted);
                Assert.IsTrue(responseMessage == null);
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
                JarRunner.WriteToProcess("TestPingJavaUdp-stop");
            }
        }

        private void DataReceived(object sender, DataReceivedEventArgs args)
        {
            // parse for the server PeerAddress, for this, it's required to 
            // know how it will be printed to the Java log

            // use a TCS to set the found address -> the calling method should await this result before proceeding

            if (args.Data != null && args.Data.Contains("[---RESULT-READY---]"))
            {
                var bytes = JarRunner.ReadJavaResult("TestPingJavaUdp-start");
                var sbytes = bytes.ToSByteArray();
                _tcs.SetResult(new PeerAddress(sbytes));
            }
        }
    }
}
