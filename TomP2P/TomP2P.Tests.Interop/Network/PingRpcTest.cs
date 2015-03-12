using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Core.Connection;
using TomP2P.Core.Message;
using TomP2P.Core.P2P;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Extensions;

namespace TomP2P.Tests.Interop.Network
{
    [TestFixture]
    public class PingRpcTest
    {
        private TaskCompletionSource<PeerAddress> _tcs;

        #region Pings from .Net to Java

        [Test]
        public async void TestPingUdpToJava()
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
                Assert.IsTrue(responseMessage.Type == Message.MessageType.Ok);
                Assert.IsTrue(responseMessage.Command == Core.Rpc.Rpc.Commands.Ping.GetNr());
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
        public async void TestPingBroadcastUdpToJava()
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
                Assert.IsTrue(responseMessage.Type == Message.MessageType.Ok);
                Assert.IsTrue(responseMessage.Command == Core.Rpc.Rpc.Commands.Ping.GetNr());
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
        public async void TestFireUdpToJava()
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
        public async void TestPingTcpToJava()
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
                Assert.IsTrue(responseMessage.Type == Message.MessageType.Ok);
                Assert.IsTrue(responseMessage.Command == Core.Rpc.Rpc.Commands.Ping.GetNr());
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
        public void TestFireTcpToJava()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TestPingUdpDiscoverToJava()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TestPingTcpDiscoverToJava()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TestPingUdpProbeToJava()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TestPingTcpProbeToJava()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Pings from Java to .NET

        [Test]
        public void TestPingUdpFromJava()
        {
            // setup .NET server and provide it's PeerAddress
            Peer receiver = null;
            try
            {
                receiver = new PeerBuilder(new Number160("0x1234")).
                    SetP2PId(55).
                    SetPorts(7777).
                    Start();

                var bytes = receiver.PeerAddress.ToByteArray().ToByteArray();
                var res = JarRunner.WriteBytesAndTestInterop(bytes);

                Assert.IsTrue(res);
                // debug server-side and analyse logs for testing
            }
            finally
            {
                if (receiver != null)
                {
                    receiver.ShutdownAsync().Wait();
                }
            }
        }

        [Test]
        public async void TestPingBroadcastUdpFromJava()
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
                Assert.IsTrue(responseMessage.Type == Message.MessageType.Ok);
                Assert.IsTrue(responseMessage.Command == Core.Rpc.Rpc.Commands.Ping.GetNr());
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
        public async void TestFireUdpFromJava()
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
        public void TestPingTcpFromJava()
        {
            // setup .NET server and provide it's PeerAddress
            Peer receiver = null;
            try
            {
                receiver = new PeerBuilder(new Number160("0x1234")).
                    SetP2PId(55).
                    SetPorts(7777).
                    Start();

                var bytes = receiver.PeerAddress.ToByteArray().ToByteArray();
                var res = JarRunner.WriteBytesAndTestInterop(bytes);

                Assert.IsTrue(res);
                // debug server-side and analyse logs for testing
            }
            finally
            {
                if (receiver != null)
                {
                    receiver.ShutdownAsync().Wait();
                }
            }
        }

        [Test]
        public void TestFireTcpFromJava()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TestPingUdpDiscoverFromJava()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TestPingTcpDiscoverFromJava()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TestPingUdpProbeFromJava()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TestPingTcpProbeFromJava()
        {
            throw new NotImplementedException();
        }

        #endregion

        private void DataReceived(object sender, DataReceivedEventArgs args)
        {
            // parse for the server PeerAddress, for this, it's required to 
            // know how it will be printed to the Java log

            // use a TCS to set the found address -> the calling method should await this result before proceeding

            if (args.Data != null && args.Data.Contains("[---RESULT-READY---]"))
            {
                var bytes = JarRunner.ReadJavaResult("JavaServerAddress");
                var sbytes = bytes.ToSByteArray();
                _tcs.SetResult(new PeerAddress(sbytes));
            }
        }
    }
}
