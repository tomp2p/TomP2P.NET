using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    public class MyTcpServer
    {
        private readonly IPEndPoint _localEndPoint;
        private readonly int _maxNrOfClients;
        private readonly TcpListener _tcpServerSocket;

        private volatile bool _isStopped; // volatile!

        public MyTcpServer(IPEndPoint localEndPoint, int maxNrOfClients)
        {
            _localEndPoint = localEndPoint;
            _maxNrOfClients = maxNrOfClients;
            _tcpServerSocket = new TcpListener(localEndPoint);
        }

        public void Start()
        {
            _tcpServerSocket.Start();

            // accept MaxNrOfClients simultaneous connections
            for (int i = 0; i < _maxNrOfClients; i++)
            {
                ServiceLoopAsync();
            }
            _isStopped = false;
        }

        public void Stop()
        {
            _tcpServerSocket.Stop();
            // TODO notify async wait in service loop (CancellationToken)
            _isStopped = true;
        }

        protected async Task ServiceLoopAsync()
        {
            // buffers
            var recvBuffer = new byte[256];
            var sendBuffer = new byte[256];

            while (!_isStopped)
            {
                // accept a client connection
                var client = await _tcpServerSocket.AcceptTcpClientAsync();
                
                // get stream for reading and writing
                var stream = client.GetStream();

                // loop to receive all data sent by the client
                while (await stream.ReadAsync(recvBuffer, 0, recvBuffer.Length) != 0)
                {
                    // process data
                    // TODO

                    // send back
                    await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);
                }
            }
        }

        private byte[] UdpPipeline(byte[] recvBytes, IPEndPoint recipient, IPEndPoint sender)
        {
            // 1. decode incoming message
            // 2. hand it to the Dispatcher
            // 3. encode outgoing message
            var recvMessage = _decoder.Read(recvBytes, recipient, sender);

            // null means that no response is sent back
            // TODO does this mean that we can close channel?
            var responseMessage = _dispatcher.RequestMessageReceived(recvMessage, true, _udpServerSocket.Client);

            // TODO channel might have been closed, check

            var buffer = _encoder.Write(responseMessage);
            var sendBytes = ConnectionHelper.ExtractBytes(buffer);
            return sendBytes;
        }

        public Socket Socket
        {
            get { return _tcpServerSocket.Server; }
        }
    }
}
