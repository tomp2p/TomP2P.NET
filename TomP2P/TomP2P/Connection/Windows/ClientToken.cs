using System.Net;
using System.Net.Sockets;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Holds the necessary information for a server-side session.
    /// </summary>
    public class ClientToken
    {
        /// <summary>
        /// Used for TCP connections. Represents the handler socket for the accepted client connection.
        /// </summary>
        public Socket ClientHandler;

        /// <summary>
        /// Used for UDP connections. Represents the remote end point from which to receive or sent to.
        /// </summary>
        public EndPoint RemotEndPoint;

        public byte[] SendBuffer { get; private set; }
        public byte[] RecvBuffer { get; private set; }

        public ClientToken(int bufferSize)
        {
            SendBuffer = new byte[bufferSize];
            RecvBuffer = new byte[bufferSize];
        }

        public void Reset()
        {
            ClientHandler = null;
            RemotEndPoint = null;
            SendBuffer = new byte[SendBuffer.Length];
            RecvBuffer = new byte[RecvBuffer.Length];
        }
    }
}
