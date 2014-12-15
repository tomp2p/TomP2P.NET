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

        private int _bufferSize = 1024;
        private string _hostName = "localhost"; // or IPAddress 127.0.0.1
        private short _serverPort = 5150;

        // TODO make server protocol-generic
        private SocketType _socketType; // TCP: Stream, UDP: Dgram
        private ProtocolType _protocolType; // TCP: Tcp, UDP: Udp

        public byte[] SendBuffer { get; set; }
        public byte[] RecvBuffer { get; set; }

        public void StartTcp()
        {
            try
            {
                // establish the local endpoint for the socket
                IPHostEntry ipHostInfo = Dns.GetHostEntry(_hostName);
                IPAddress localAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEp = new IPEndPoint(localAddress, _serverPort);

                // create a TCP/IP server socket
                Socket server = new Socket(localAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Socket handler = null;

                // BINDING
                try
                {
                    server.Bind(localEp);
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

                // establish the local endpoint for the socket
                IPHostEntry ipHostInfo = Dns.GetHostEntry(_hostName);
                IPAddress localAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEp = new IPEndPoint(localAddress, _serverPort);
                IPEndPoint senderAddress = new IPEndPoint(localAddress, 0);

                // create a UDP/IP server socket
                Socket server = new Socket(localAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                Socket handler = null;

                // BINDING
                try
                {
                    server.Bind(localEp);
                }
                catch (Exception)
                {
                    throw new Exception("Binding failed.");
                }

                // IOControl
                try
                {
                    // TODO needed?
                    byte[] optionIn = new byte[] {0, 0, 0, 1}; // true
                    server.IOControl(SioUdpConnreset, optionIn, null);
                }
                catch (Exception)
                {
                    throw new Exception("IOControl failed.");
                }

                // RECEIVING
                try
                {
                    EndPoint ep = senderAddress;
                    server.ReceiveFrom(RecvBuffer, ref ep);
                }
                catch (Exception)
                {
                    throw new Exception("Receiving failed.");
                }

                // Manipulate Data
                SendBuffer = RecvBuffer;

                // SENDING
                try
                {
                    int bytesRecv = server.SendTo(SendBuffer, senderAddress);

                    // send zero byte datagrams to indicate end
                    // multiple packets are send to raise the pronbability that the client gets one
                    for (int i = 0; i < 3; i++)
                    {
                        server.SendTo(SendBuffer, 0, 0, SocketFlags.None, senderAddress);
                        Thread.Sleep(250);
                    }

                    // TODO close socket? server needs to remain available though
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
    }
}
