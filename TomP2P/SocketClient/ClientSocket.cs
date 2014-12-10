using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
