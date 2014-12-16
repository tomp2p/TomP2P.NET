using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Sockets
{
    /// <summary>
    /// Synchronous server socket that suspends execution of the server application
    /// while waiting for a connection from a client.
    /// </summary>
    public class SyncServer
    {
        /// <summary>
        /// Winsock ioctl code which will disable ICMP errors from being propagated to a UDP socket.
        /// This can occur if a UDP packet is sent to a valid destination, but there is no socket
        /// registered to listen on the given port.
        /// </summary>
        public const int SioUdpConnreset = -1744830452;


        public byte[] SendBuffer { get; set; }
        public byte[] RecvBuffer { get; set; }

        private static IPAddress _localAddress = IPAddress.Any; // wildcard
        static private short _serverPort = 5151;
        IPEndPoint _localEp = new IPEndPoint(_localAddress, _serverPort);

        public void StartTcp()
        {
            try
            {
                // create a TCP/IP server socket
                Socket server = new Socket(_localAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Socket handler = null;

                // BINDING
                try
                {
                    server.Bind(_localEp);
                }
                catch (Exception)
                {
                    throw new Exception("Binding failed.");
                }

                // LISTENING
                try
                {
                    // TCP only
                    server.Listen(10); // TODO find appropriate backlog
                }
                catch (Exception)
                {
                    throw new Exception("Listening failed.");
                }

                // TODO servicing loop
                // ACCEPTING (RECEIVING)
                try
                {
                    handler = server.Accept(); // blocking

                    while (true)
                    {
                        int bytesRecv = handler.Receive(RecvBuffer);

                        if (bytesRecv == 0)
                        {
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    throw new Exception("Accepting/Receiving failed.");
                }

                // Manipulate Data
                SendBuffer = RecvBuffer;

                // SENDING
                try
                {
                    handler.Send(SendBuffer);

                    // shutdown handler/client-connection
                    handler.Shutdown(SocketShutdown.Send);
                    handler.Close();
                }
                catch (Exception)
                {
                    throw new Exception("Sending failed.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void StartUdp()
        {
            try
            {
                // create a UDP/IP server socket
                Socket server = new Socket(_localAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                // BINDING
                try
                {
                    server.Bind(_localEp);
                }
                catch (Exception ex)
                {
                    throw new Exception("Binding failed.", ex);
                }

                // IOControl
                try
                {
                    // TODO needed?
                    byte[] optionIn = new byte[] { 0, 0, 0, 1 }; // true
                    server.IOControl(SioUdpConnreset, optionIn, null);
                }
                catch (Exception)
                {
                    throw new Exception("IOControl failed.");
                }

                // service clients in a loop
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remoteEp = (EndPoint) sender;
                while (true)
                {
                    // RECEIVING
                    try
                    {
                        int bytesRecv = server.ReceiveFrom(RecvBuffer, ref remoteEp);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Receiving failed.", ex);
                    }

                    // Manipulate Data
                    SendBuffer = RecvBuffer;

                    // SENDING
                    try
                    {
                        //while (true)
                        //{
                        // multiple packets are send to raise the probability that the client gets one

                        for (int i = 0; i < 10; i++)
                        {
                            int bytesSent = server.SendTo(SendBuffer, remoteEp);
                        }

                        // send zero byte datagrams to indicate end
                        /*for (int i = 0; i < 10; i++)
                        {
                            server.SendTo(SendBuffer, 0, 0, SocketFlags.None, remoteEp);
                            // TODO thread sleep?
                        }*/
                        //}
                    }
                    catch (Exception)
                    {
                        throw new Exception("Sending failed.");
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
