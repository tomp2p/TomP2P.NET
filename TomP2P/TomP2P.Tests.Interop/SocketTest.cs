using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Extensions.Sockets;

namespace TomP2P.Tests.Interop
{
    [TestFixture]
    public class SocketTest
    {
        [Test]
        public void TcpSocketTest()
        {
            // create sample msg
            var msg = MessageEncodeDecodeTest.CreateMessageInteger();
            var bytes = MessageEncodeDecodeTest.EncodeMessage(msg);

            // start server socket on a separate thread
            var server = new SyncServer();
            server.SendBuffer = new byte[bytes.Length];
            server.RecvBuffer = new byte[bytes.Length];

            new Thread(server.StartTcp).Start();

            // start client socket
            var client = new SyncClient();
            client.SendBuffer = bytes;
            client.RecvBuffer = new byte[bytes.Length];

            client.StartTcp();

            Assert.AreEqual(client.SendBuffer, client.RecvBuffer);
        }
        
        [Test]
        public void UdpSocketTest()
        {
            // create sample msg
            var msg = MessageEncodeDecodeTest.CreateMessageInteger();
            var bytes = MessageEncodeDecodeTest.EncodeMessage(msg);

            // start server socket on a separate thread
            var server = new SyncServer();
            server.SendBuffer = new byte[bytes.Length];
            server.RecvBuffer = new byte[bytes.Length];

            new Thread(server.StartUdp).Start();

            // start client socket
            var client = new SyncClient();
            client.SendBuffer = bytes;
            client.RecvBuffer = new byte[bytes.Length];

            client.StartUdp();

            Assert.AreEqual(client.SendBuffer, client.RecvBuffer);
        }
    }
}
