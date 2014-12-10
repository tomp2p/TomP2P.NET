using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Winsocks
{
    /*
    This is a simple TCP and UDP server. A socket of the requested type is created that waits for clients.
    For TCP, the server waits for an incoming TCP connection after which it receives a "request".
    The request is terminated by the client shutting down the connection. After the request is received, the
    server sends data in response followed by shutting down its connection and closing the socket.
    The UDP server simply waits for a datagram request.
    The request consists of a single datagram packet. The server then sends a number of responses to the source
    address of the request followed by a number of zero byte datagrams. The zero byte datagrams will indicate 
    to the client that no more data will follow.
     
    Usage:
      Executable_file_name [-| bind-address] [-m message] [-n count] [-p port] [-t tcp|udp] [-x size]
    
      -| bind-address   local address to bind to
      -m message        text message to format into send buffer
      -n count          number of times to send a message
      -p port           local port to bind to
      -t tcp|udp        indicates which protocol to use
      -x size           size of send and receive buffer
    */

    /// <summary>
    /// This is a simple TCP and UDP based server.
    /// </summary>
    public class ServerSocket
    {
        /// <summary>
        /// Winsock ioctl code which will disable ICMP errors from being propagated to a UDP socket.
        /// This can occur if a UDP packet is sent to a valid destination, but there is no socket
        /// registered to listen on the given port.
        /// </summary>
        public const int SioUdpConnreset = -1744830452;

        static void Main(string[] args)
        {
            string txtMessage = "Server: ServerResponse";
            int localPort = 5150;
            int sendCount = 10;
            int bufferSize = 4096;

            IPAddress localAddress = IPAddress.Any;
            SocketType sockType = SocketType.Stream;
            ProtocolType sockProtcol = ProtocolType.Tcp;

            PrintUsage();

            // parse command line
            for (int i = 0; i < args.Length; i++)
            {
                try
                {
                    if (args[i][0] == '-')
                    {
                        switch (Char.ToLower(args[i][1]))
                        {
                            case '|':
                                localAddress = IPAddress.Parse(args[++i]);
                                break;
                            case 'm':
                                txtMessage = args[++i];
                                break;
                            case 'n':
                                sendCount = Convert.ToInt32(args[++i]);
                                break;
                            case 'p':
                                localPort = Convert.ToInt32(args[++i]);
                                break;
                            case 't':
                                i++;
                                if (String.Compare(args[i], "tcp", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    sockType = SocketType.Stream;
                                    sockProtcol = ProtocolType.Tcp;
                                }
                                else if (String.Compare(args[i], "udp", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    sockType = SocketType.Dgram;
                                    sockProtcol = ProtocolType.Udp;
                                }
                                else
                                {
                                    PrintUsage();
                                    return;
                                }
                                break;
                            case 'x':
                                bufferSize = Convert.ToInt32(args[++i]);
                                break;
                            default:
                                PrintUsage();
                                return;
                        }
                    }
                }
                catch (Exception)
                {
                    PrintUsage();
                    return;
                }
            }

            Socket serverSocket = null;

            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(localAddress, localPort);
                IPEndPoint senderAddress = new IPEndPoint(localAddress, 0);
                Console.WriteLine("Server: IPEndPoint is OK.");

                EndPoint castSenderAddress;
                Socket clientSocket;

                byte[] receiveBuffer = new byte[bufferSize];
                byte[] sendBuffer = new byte[bufferSize];
                int rc;
                FormatBuffer(sendBuffer, txtMessage);

                // create the server socket
                serverSocket = new Socket(localAddress.AddressFamily, sockType, sockProtcol);
                Console.WriteLine("Server: Socket() is OK.");

                // bind the server socket to the specified local interface
                serverSocket.Bind(localEndPoint);
                Console.WriteLine("Server: {0} server socket bound to {1}.", sockProtcol, localEndPoint);

                if (sockProtcol == ProtocolType.Tcp)
                {
                    // if TCP socket, set the socket to listening
                    serverSocket.Listen(1);
                    Console.WriteLine("Server: Listen() is OK.");
                }
                else
                {
                    byte[] byteTrue = new byte[4];
                    byteTrue[byteTrue.Length - 1] = 1;

                    serverSocket.IOControl(SioUdpConnreset, byteTrue, null);
                    Console.WriteLine("Server: IOControl() is OK.");
                }

                // service clients in a loop
                while (true)
                {
                    // TCP
                    if (sockProtcol == ProtocolType.Tcp)
                    {
                        // wait for a client connection
                        clientSocket = serverSocket.Accept();
                        Console.WriteLine("Server: Accept() is OK.");
                        Console.WriteLine("Server: Accepted connection from {0}.", clientSocket.RemoteEndPoint);

                        // receive the request from the client in a loop until the client
                        // shuts the connection down
                        Console.WriteLine("Server: Preparing to receive using Receive()...");
                        while (true)
                        {
                            rc = clientSocket.Receive(receiveBuffer);
                            Console.WriteLine("Server: Read {0} bytes.", rc);
                            if (rc == 0)
                            {
                                break;
                            }
                        }

                        // send the indicated number of response messages
                        Console.WriteLine("Server: Preparing to send using Send()...");
                        for (int i = 0; i < sendCount; i++)
                        {
                            rc = clientSocket.Send(sendBuffer);
                            Console.WriteLine("Server: Sent {0} bytes.", rc);
                        }

                        // shutdown the client connection
                        clientSocket.Shutdown(SocketShutdown.Send);
                        Console.WriteLine("Server: Shutdonw() is OK.");
                        clientSocket.Close();
                        Console.WriteLine("Server: Close() is OK.");
                    }
                    // UDP
                    else
                    {
                        castSenderAddress = (EndPoint) senderAddress;

                        // receive the initial request from the client
                        rc = serverSocket.ReceiveFrom(receiveBuffer, ref castSenderAddress);
                        Console.WriteLine("Server: ReceiveFrom() is OK.");
                        senderAddress = (IPEndPoint) castSenderAddress;
                        Console.WriteLine("Server: Received {0} bytes from {1}.", rc, senderAddress);

                        // send the response to the client the requested number of times
                        for (int i = 0; i < sendCount; i++)
                        {
                            try
                            {
                                rc = serverSocket.SendTo(sendBuffer, senderAddress);
                                Console.WriteLine("Server: SendTo() is OK.");
                            }
                            catch (Exception)
                            {
                                // TODO blabla
                                continue;
                            }
                            Console.WriteLine("Server: Sent {0} bytes to {1}.", rc, senderAddress);
                        }

                        // send several zero byte datagrams to indicate to the client that no
                        // more data will be sent from the server
                        // multiple packets are sent since UDP is not guaranteed and we want
                        // to try to make an effort the client gets a least one
                        Console.WriteLine("Server: Preparing to send SendTo(), on the way do sleeping...");
                        for (int i = 0; i < 3; i++)
                        {
                            serverSocket.SendTo(sendBuffer, 0, 0, SocketFlags.None, senderAddress);
                            Thread.Sleep(250);
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Server: Socket error occurred: {0}", ex.Message);
            }
            finally
            {
                // close the socket if necessary
                if (serverSocket != null)
                {
                    Console.WriteLine("Server: Closing using Close()...");
                    serverSocket.Close();
                }
            }
        }

        public static void PrintUsage()
        {
            Console.WriteLine(
                "\nExecutable_file_name [-| bind-address);] [-m message] [-n count] [-p port] [-t tcp|udp] [-x size]");
            Console.WriteLine("-| bind-address   local address to bind to");
            Console.WriteLine("-m message        text message to format into send buffer");
            Console.WriteLine("-n count          number of times to send a message");
            Console.WriteLine("-p port           local port to bind to");
            Console.WriteLine("-t tcp|udp        indicates which protocol to use");
            Console.WriteLine("-x size           size of send and receive buffer");
            Console.WriteLine("Else, default values will be used.\n");
        }

        /// <summary>
        /// Repeatedly copies a message into a byte array until filled.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <param name="message"></param>
        public static void FormatBuffer(byte[] dataBuffer, string message)
        {
            byte[] byteMessage = Encoding.ASCII.GetBytes(message);
            int index = 0;

            while (index < dataBuffer.Length)
            {
                for (int j = 0; j < byteMessage.Length; j++)
                {
                    dataBuffer[index] = byteMessage[j];
                    index++;

                    if (index >= dataBuffer.Length)
                    {
                        break;
                    }
                }
            }
        }
    }
}
