using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Connection.Windows;
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

        [Test]
        public void TcpAsyncSocketTest()
        {
            var r = new Random();

            const int bufferSize = 256;
            var serverEp = new IPEndPoint(IPAddress.Any, 5151);

            // start server socket on a separate thread
            var server = new AsyncSocketServer(4, bufferSize);
            new Thread(() => server.Start(serverEp)).Start();

            // prepare 4 async clients
            var clients = new AsyncSocketClient[4];
            var tasks = new Task[clients.Length];
            const int iterations = 10;
            var results = new bool[clients.Length][];
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i] = new AsyncSocketClient("localhost", 5151 + i);
                results[i] = new bool[iterations];
            }

            // run the async clients on separate threads
            for (int i = 0; i < clients.Length; i++)
            {
                int i1 = i;
                var t = Task.Run(() =>
                {
                    clients[i1].Connect();

                    // iterations
                    for (int j = 0; j < iterations; j++)
                    {
                        var sendBytes = new byte[bufferSize];
                        r.NextBytes(sendBytes);
                        var recvBytes = clients[i1].SendReceive(sendBytes);
                        results[i1][j] = sendBytes.Equals(recvBytes);
                    }

                    clients[i1].Disconnect();
                });
                tasks[i] = t;
            }

            // await all tasks
            Task.WaitAll(tasks);

            // check all results for true
            for (int i = 0; i < results.GetLength(0); i++)
            {
                for (int j = 0; j < results.GetLength(1); j++)
                {
                    Assert.IsTrue(results[i][j]);
                }
            }
        }
    }
}
