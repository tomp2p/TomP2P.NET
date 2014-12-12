using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Sockets
{
    /// <summary>
    /// Synchronous client socket that suspends execution of the client application
    /// until the server returns a response.
    /// </summary>
    public class SyncClient
    {
        private const int _bufferSize = 1024;
        private string _hostName = "localhost"; // or IPAddress 127.0.0.1
        private short _serverPort = 5150;

        // TODO make client protocol-generic
        private SocketType _socketType; // TCP: Stream, UDP: Dgram
        private ProtocolType _protocolType; // TCP: Tcp, UDP: Udp

        public byte[] SendBuffer { get; set; }
        public byte[] RecvBuffer { get; set; }

        public void Start()
        {
            try
            {
                Socket sender = null;
                IPEndPoint remoteEp = null;
                IPHostEntry ipHostInfo = Dns.GetHostEntry(_hostName);

                // CONNECT
                // try each address
                foreach (var address in ipHostInfo.AddressList)
                {
                    sender = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        // create a TCP/IP socket
                        remoteEp = new IPEndPoint(address, _serverPort);
                        sender.Connect(remoteEp);
                        break;
                    }
                    catch (SocketException)
                    {
                        // connect failed, try next address
                    }
                }
                if (sender == null || remoteEp == null)
                {
                    throw new Exception("Establishing connection failed.");
                }

                // SEND
                try
                {
                    // send the data throug the socket
                    int bytesSent = sender.Send(SendBuffer);

                    // shutdown sending on client-side
                    sender.Shutdown(SocketShutdown.Send); // TCP-only
                }
                catch (Exception)
                {
                    throw new Exception("Sending failed.");
                }

                // RECEIVE
                try
                {
                    while (true)
                    {
                        int bytesRecv = sender.Receive(RecvBuffer); // blocking
                    
                        // exit loop if server indicates shutdown
                        if (bytesRecv == 0)
                        {
                            // shutdown client
                            // TODO sender.Shutdown(SocketShutdown.Send); needed?
                            sender.Close();
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    throw new Exception("Receiving failed.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
