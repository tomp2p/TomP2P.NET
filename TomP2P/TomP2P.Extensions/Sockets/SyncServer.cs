using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Sockets
{
    /// <summary>
    /// Synchronous server socket that suspends execution of the server application
    /// while waiting for a connection from a client.
    /// </summary>
    public class SyncServer
    {
        private int _bufferSize = 1024;
        private string _hostName = "localhost"; // or IPAddress 127.0.0.1
        private short _serverPort = 5150;

        // TODO make server protocol-generic
        private SocketType _socketType; // TCP: Stream, UDP: Dgram
        private ProtocolType _protocolType; // TCP: Tcp, UDP: Udp

        public void Start()
        {
            byte[] bytes = new byte[_bufferSize];

            // establish the local endpoint for the socket
            IPHostEntry ipHostInfo = Dns.GetHostEntry(_hostName);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEp = new IPEndPoint(ipAddress, _serverPort);

            // create a TCP/IP socket
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // bind the socket to the local endpoint and listen for incoming connections
            try
            {
                listener.Bind(localEp);
                listener.Listen(10); // TODO select optimal backlog

                while (true)
                {
                    Socket handler = listener.Accept(); // blocking

                    // incoming connection needs to be processed
                    while (true)
                    {
                        // TODO prepare receive buffer
                        bytes = new byte[_bufferSize];

                        int bytesRec = handler.Receive(bytes);

                        // TODO detect end
                        if (bytesRec == 0)
                        {
                            break;
                        }
                    }

                    // return response
                    // TODO prepare send buffer
                    handler.Send(bytes);
                    handler.Shutdown(SocketShutdown.Send);
                    handler.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
