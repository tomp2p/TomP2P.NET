using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Peers
{
    // TODO implement Serializable?
    public class PeerSocketAddress
    {
        public PeerSocketAddress(IPAddress inetAddress, int tcpPort, int udpPort)
        {
            throw new NotImplementedException();
        }

        public int Offset { get; private set; }
        public IPAddress InetAddress { get; private set; }
        public int TcpPort { get; private set; }
        public int UdpPort { get; private set; }

        public int Size
        {
            // TODO implement
            get { throw new NotImplementedException(); }
        }

        public bool IsIPv4 { get; set; }


        public static PeerSocketAddress Create(byte[] me, bool isIPv4, int offsetOriginal)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public int ToByteArray(byte[] me, int newOffset)
        {
            throw new NotImplementedException();
        }

        internal byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }
    }
}
