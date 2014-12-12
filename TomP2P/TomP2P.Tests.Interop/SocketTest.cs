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
        public void SendReceiveMessageTest()
        {
            // create sample msg
            var msg = MessageEncodeDecodeTest.CreateMessageInteger();

            // start server socket on a separate thread
            var t = new Thread(new SyncServer().Start);
            t.Start();

            // start client socket
            var client = new SyncClient();
            var bytes = MessageEncodeDecodeTest.EncodeMessage(msg);
            client.SendBuffer = bytes;
            client.RecvBuffer = new byte[bytes.Length];

            client.Start();

            Assert.AreEqual(client.SendBuffer, client.RecvBuffer);
        }
    }
}
