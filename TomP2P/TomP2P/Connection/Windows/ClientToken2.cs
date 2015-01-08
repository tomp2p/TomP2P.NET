using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Holds the necessary information for a server-side session.
    /// </summary>
    public class ClientToken2
    {
        /// <summary>
        /// Used for TCP connections. Represents the handler socket for the accepted client connection.
        /// </summary>
        public Socket ClientHandler;

        /// <summary>
        /// Used for UDP connections. Represents the remote end point from which to receive or sent to.
        /// </summary>
        public IPEndPoint RemotEndPoint;


        public void Reset()
        {
            ClientHandler = null;
            RemotEndPoint = null;
        }
    }
}
