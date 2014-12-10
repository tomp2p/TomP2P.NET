using System;
using System.Net;
using System.Net.Sockets;

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
        // TODO SIO_UDP_CONNRESET needed?

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

            }
            catch (SocketException ex)
            {
                Console.WriteLine("Server: Socket error occurred: {0}", ex.Message);
                throw;
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
            Console.WriteLine();
            Console.WriteLine(
                "Executable_file_name [-| bind-address);] [-m message] [-n count] [-p port] [-t tcp|udp] [-x size]");
            Console.WriteLine("-| bind-address   local address to bind to");
            Console.WriteLine("-m message        text message to format into send buffer");
            Console.WriteLine("-n count          number of times to send a message");
            Console.WriteLine("-p port           local port to bind to");
            Console.WriteLine("-t tcp|udp        indicates which protocol to use");
            Console.WriteLine("-x size           size of send and receive buffer");
            Console.WriteLine();
        }
    }
}
