using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Winsocks;

namespace SocketClient
{
    /*
    This is a simple TCP and UDP client application.
    For TCP, the server name is resolved and a socket is created to attempt a connection
    to each address returned until a connection succeeds.
    */

    /// <summary>
    /// This is a simple TCP and UDP based client.
    /// </summary>
    public class ClientSocket
    {
        static void Main(string[] args)
        {
            SocketType sockType = SocketType.Stream;
            ProtocolType sockProtocol = ProtocolType.Tcp;
            string remoteName = "localhost";
            string txtMessage = "Client: Test Request.";
            bool udpConnect = false;
            int remotePort = 5150;
            int bufferSize = 4096;

            PrintUsage();

            // parse the command line
            for (int i = 0; i < args.Length; i++)
            {
                try
                {
                    if ((args[i][0] == '-'))
                    {
                        switch (Char.ToLower(args[i][1]))
                        {
                            case 'c':
                                udpConnect = true;
                                break;
                            case 'n':
                                remoteName = args[++i];
                                break;
                            case 'm':
                                txtMessage = args[++i];
                                break;
                            case 'p':
                                remotePort = System.Convert.ToInt32(args[++i]);
                                break;
                            case 't':
                                i++;
                                if (String.Compare(args[i], "tcp", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    sockType = SocketType.Stream;
                                    sockProtocol = ProtocolType.Tcp;
                                }
                                else if (String.Compare(args[i], "udp", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    sockType = SocketType.Dgram;
                                    sockProtocol = ProtocolType.Udp;
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
                catch
                {
                    PrintUsage();
                    return;
                }
            }

            Socket clientSocket = null;
            IPHostEntry resolvedHost = null;
            IPEndPoint destination = null;
            byte[] sendBuffer = new byte[bufferSize];
            byte[] recvBuffer = new byte[bufferSize];
            int rc;
            ServerSocket.FormatBuffer(sendBuffer, txtMessage);

            try
            {
                // try to resolve the remote host name or address
                resolvedHost = Dns.GetHostEntry(remoteName);
                Console.WriteLine("Client: GetHostEntry() is OK.");

                // try each returned address
                foreach (IPAddress address in resolvedHost.AddressList)
                {
                    // create a socket corresponding to the address family of the resolved address
                    clientSocket = new Socket(address.AddressFamily, sockType, sockProtocol);
                    Console.WriteLine("Client: Socket() is OK.");

                    try
                    {
                        // create the endpoint that describes the destination
                        destination = new IPEndPoint(address, remotePort);
                        Console.WriteLine("Client: IPEndPoint() is OK. IP address: {0}, Server Port: {1}.", address,
                            remotePort);

                        if (sockProtocol == ProtocolType.Udp && udpConnect == false)
                        {
                            Console.WriteLine("Client: Destination address is: {0}.", destination);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Client: Attempting connection to {0}.", destination);
                        }
                        clientSocket.Connect(destination);
                        Console.WriteLine("Client: Connect() is OK.");
                        break;
                    }
                    catch (SocketException)
                    {
                        // connect failed, so close the socket and try the next address
                        clientSocket.Close();
                        Console.WriteLine("Client: Close() is OK.");
                        clientSocket = null;
                        continue;
                    }
                }

                // make sure we have a valid socket before trying to use it
                if (clientSocket != null && destination != null)
                {
                    try
                    {
                        // send the request to the server
                        if (sockProtocol == ProtocolType.Udp && udpConnect == false)
                        {
                            clientSocket.SendTo(sendBuffer, destination);
                            Console.WriteLine("Client: SendTo() is OK. [UDP]");
                        }
                        else
                        {
                            rc = clientSocket.Send(sendBuffer);
                            Console.WriteLine("Client: Send() is OK. [TCP]");
                            Console.WriteLine("Client: Sent request of {0} bytes.", rc);

                            // for TCP, shutdown sending on our side since the client
                            // won't send any more data
                            if (sockProtocol == ProtocolType.Tcp)
                            {
                                clientSocket.Shutdown(SocketShutdown.Send);
                                Console.WriteLine("Client: Shutdown() is OK.");
                            }
                        }

                        // receive data in a loop until the server closes the connection
                        // for TCP, this occurs when the server performs a shutdown or 
                        // closes the socket
                        // for UDP, we will know to exit when the remote host sends a
                        // zero byte datagram
                        while (true)
                        {
                            if (sockProtocol == ProtocolType.Tcp || udpConnect == true)
                            {
                                rc = clientSocket.Receive(recvBuffer);
                                Console.WriteLine("Client: Receive() is OK.");
                                Console.WriteLine("Client: Read {0} bytes.", rc);
                            }
                            else
                            {
                                IPEndPoint fromEndPoint = new IPEndPoint(destination.Address, 0);
                                Console.WriteLine("Client: IPEndPoint is OK.");
                                EndPoint castFromEndPoint = (EndPoint) fromEndPoint;
                                rc = clientSocket.ReceiveFrom(recvBuffer, ref castFromEndPoint);
                                Console.WriteLine("Client: ReceiveFrom() is OK.");
                                fromEndPoint = (IPEndPoint) castFromEndPoint;
                                Console.WriteLine("Client: Read {0} bytes from {1}.", rc, fromEndPoint);
                            }

                            // exit loop if server indicates shutdown
                            if (rc == 0)
                            {
                                clientSocket.Close();
                                Console.WriteLine("Client: Close() is OK.");
                                break;
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine("Client: Error occurred while sending or receiving data.");
                        Console.WriteLine("-> Error: {0}", ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Client: Unable to establish connection to server!");
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Client: Socket error occurred: {0}", ex.Message);
            }
        }

        public static void PrintUsage()
        {
            Console.WriteLine("\nExecutable_file_name [-c] [-n server] [-p port] [-m message] [-t tcp|udp] [-x size]");
            Console.WriteLine("-c           if UDP connect the socket before sending");
            Console.WriteLine("-n server    server name or address to connect/send to");
            Console.WriteLine("-p port      port number to connect/send to");
            Console.WriteLine("-m           message to format in request buffer");
            Console.WriteLine("-t tcp|udp        indicates which protocol to use");
            Console.WriteLine("-x size           size of send and receive buffer");
            Console.WriteLine("Else, default values will be used.\n");
        }
    }
}
