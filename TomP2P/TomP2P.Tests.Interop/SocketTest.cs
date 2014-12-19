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

        /*[Test]
        public void TcpAsyncSocketTest()
        {
            var r = new Random();
            const int iterations = 3;
            const int nrOfClients = 2;
            const int bufferSize = 10;
            const string serverName = "localhost";
            const int serverPort = 5151;
            var serverEp = new IPEndPoint(IPAddress.Any, serverPort);

            // start server socket on a separate thread
            var server = new AsyncSocketServer(nrOfClients, bufferSize);
            new Thread(() => server.Start(serverEp)).Start();

            // prepare async clients
            var tasks = new Task[nrOfClients];
            var results = new bool[nrOfClients][];
            for (int i = 0; i < nrOfClients; i++)
            {
                results[i] = new bool[iterations];
            }

            // run the async clients on separate threads
            for (int i = 0; i < nrOfClients; i++)
            {
                int i1 = i;
                var t = Task.Run(() =>
                {
                    using (var client = new AsyncSocketClient(serverName, serverPort, bufferSize))
                    {
                        client.Connect();

                        // iterations
                        for (int j = 0; j < iterations; j++)
                        {
                            var sendBytes = new byte[bufferSize];
                            r.NextBytes(sendBytes);
                            var recvBytes = client.SendReceive(sendBytes);
                            var res = sendBytes.SequenceEqual(recvBytes);
                            results[i1][j] = res;
                        }

                        client.Disconnect();
                    }
                });
                tasks[i] = t;
            }

            // await all tasks
            Task.WaitAll(tasks);

            server.Stop();

            // check all results for true
            for (int i = 0; i < results.Length; i++)
            {
                for (int j = 0; j < results[i].Length; j++)
                {
                    Assert.IsTrue(results[i][j]);
                }
            }
        }*/

        [Test]
        public void TcpAsyncSocket2Test()
        {
            var r = new Random();
            const int iterations = 1;
            const int nrOfClients = 2;
            const int bufferSize = 10;

            var tasks = new Task[nrOfClients];
            var results = new bool[nrOfClients][];
            for (int i = 0; i < nrOfClients; i++)
            {
                results[i] = new bool[iterations];
            }

            const string serverName = "localhost";
            const int serverPort = 5151;
            var serverEp = new IPEndPoint(IPAddress.Any, serverPort);

            // start server socket on a separate thread
            var server = new AsyncSocketServer2(bufferSize);
            new Thread(() => server.Start(serverEp)).Start();

            // run the async clients on separate threads
            for (int i = 0; i < nrOfClients; i++)
            {
                int i1 = i;
                var t = Task.Run(async () =>
                {
                    var client = new AsyncSocketClient2(serverName, serverPort);
                    await client.ConnectAsync();
                    for (int j = 0; j < iterations; j++)
                    {
                        // send random bytes and expect same bytes as echo
                        var sendBytes = new byte[bufferSize];
                        var recvBytes = new byte[bufferSize];
                        r.NextBytes(sendBytes);
                        await client.SendAsync(sendBytes);
                        await client.ReceiveAsync(recvBytes);

                        var res = sendBytes.SequenceEqual(recvBytes);
                        results[i1][j] = res;
                    }
                    await client.DisconnectAsync();
                });
                tasks[i] = t;
            }

            // await all tasks
            Task.WaitAll(tasks);

            // check all results for true
            for (int i = 0; i < results.Length; i++)
            {
                for (int j = 0; j < results[i].Length; j++)
                {
                    Assert.IsTrue(results[i][j]);
                }
            }
        }
    }
}
