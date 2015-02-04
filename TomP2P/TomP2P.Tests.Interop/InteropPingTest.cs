using System;
using System.Diagnostics;
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
        public async void TestPingToJavaUdp()
        {
            // setup Java server and get it's PeerAddress
            _tcs = new TaskCompletionSource<PeerAddress>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                JarRunner.Run("JavaPingReceiver-start", DataReceived);
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
                Assert.IsTrue(responseMessage.Type == Message.Message.MessageType.Ok);
                Assert.IsTrue(responseMessage.Command == TomP2P.Rpc.Rpc.Commands.Ping.GetNr());
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
                JarRunner.WriteToProcess("JavaPingReceiver-stop");
            }
        }

        [Test]
        public async void TestPingToJavaBroadcastUdp()
        {
            // setup Java server and get it's PeerAddress
            _tcs = new TaskCompletionSource<PeerAddress>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                JarRunner.Run("JavaPingReceiver-start", DataReceived);
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
                Assert.IsTrue(responseMessage.Type == Message.Message.MessageType.Ok);
                Assert.IsTrue(responseMessage.Command == TomP2P.Rpc.Rpc.Commands.Ping.GetNr());
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
                JarRunner.WriteToProcess("JavaPingReceiver-stop");
            }
        }

        [Test]
        public async void TestPingToJavaFireUdp()
        {
            // TODO find a way to check whether Java side received the ff ping
            // setup Java server and get it's PeerAddress
            _tcs = new TaskCompletionSource<PeerAddress>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                JarRunner.Run("JavaPingReceiver-start", DataReceived);
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
                JarRunner.WriteToProcess("JavaPingReceiver-stop");
            }
        }

        [Test]
        public async void TestPingToJavaTcp()
        {
            // setup Java server and get it's PeerAddress
            _tcs = new TaskCompletionSource<PeerAddress>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                JarRunner.Run("JavaPingReceiver-start", DataReceived);
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

                cc = await sender.ConnectionBean.Reservation.CreateAsync(0, 1);
                var handshake = new PingRpc(sender.PeerBean, sender.ConnectionBean);
                var task = handshake.PingTcpAsync(server, cc, new DefaultConnectionConfiguration());
                var responseMessage = await task;

                Assert.IsTrue(task.IsCompleted && !task.IsFaulted);
                Assert.AreEqual(responseMessage.Sender, server);
                Assert.IsTrue(responseMessage.Type == Message.Message.MessageType.Ok);
                Assert.IsTrue(responseMessage.Command == TomP2P.Rpc.Rpc.Commands.Ping.GetNr());
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
                JarRunner.WriteToProcess("JavaPingReceiver-stop");
            }
        }

        [Test]
        public async void TestPingToJavaTcp2()
        {
            // TODO implement
            throw new NotImplementedException();
        }

        [Test]
        public void TestPingFromJavaUdp()
        {
            // setup .NET server and provide it's PeerAddress
            Peer receiver = new PeerBuilder(new Number160("0x1234")).
                SetP2PId(55).
                SetPorts(7777).
                Start();

            var bytes = receiver.PeerAddress.ToByteArray().ToByteArray();
            var res = JarRunner.WriteBytesAndTestInterop(bytes);

            Assert.IsTrue(res);
            // debug server-side and analyse logs for testing
        }

        [Test]
        public void TestPingFromJavaTcp()
        {
            // setup .NET server and provide it's PeerAddress
            Peer receiver = new PeerBuilder(new Number160("0x1234")).
                SetP2PId(55).
                SetPorts(7777).
                Start();

            var bytes = receiver.PeerAddress.ToByteArray().ToByteArray();
            var res = JarRunner.WriteBytesAndTestInterop(bytes);

            Assert.IsTrue(res);
            // debug server-side and analyse logs for testing
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
