using System.Net;
using System.Net.Sockets;

namespace TomP2P.Connection.Windows
{
    public class ClientToken
    {
        public Socket ClientHandler; // for TCP connections
        public EndPoint RemotEndPoint; // for UDP connections
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
