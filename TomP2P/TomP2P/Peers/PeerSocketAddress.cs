using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Workaround;

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

        public int ToByteArray(sbyte[] me, int newOffset)
        {
            throw new NotImplementedException();
        }

        public sbyte[] ToByteArray()
        {
            throw new NotImplementedException();
        }

        public static PeerSocketAddress Create(JavaBinaryReader buffer, bool IsIPv4)
        {
            throw new NotImplementedException();
        }

        public static int CalculateSize(bool isIPv4)
        {
            // TODO introduce constants
            return 2 + 2 + (isIPv4 ? Utils.Utils.IPv4Bytes : Utils.Utils.IPv6Bytes);
        }
    }
}
