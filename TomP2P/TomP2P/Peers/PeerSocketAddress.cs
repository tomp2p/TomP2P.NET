using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Peers
{
    public class PeerSocketAddress : IEquatable<PeerSocketAddress>
    {
        /// <summary>
        /// The inernet address, which is IPv4 or IPv6.
        /// </summary>
        public IPAddress InetAddress { get; private set; }

        /// <summary>
        /// The TCP port.
        /// </summary>
        public int TcpPort { get; private set; }

        /// <summary>
        /// The UDP port.
        /// </summary>
        public int UdpPort { get; private set; }

        /// <summary>
        /// The offset.
        /// </summary>
        public long Offset { get; private set; }
        
        /// <summary>
        /// Creates a PeerSocketAddress including both UDP and TCP ports.
        /// </summary>
        /// <param name="inetAddress">The <see cref="IPAddress"/> of the peer. Can be IPv4 or IPv6.</param>
        /// <param name="tcpPort">The TCP port.</param>
        /// <param name="udpPort">The UDP port.</param>
        public PeerSocketAddress(IPAddress inetAddress, int tcpPort, int udpPort)
            : this(inetAddress, tcpPort, udpPort, -1)
        { }

        /// <summary>
        /// Creates a PeerSocketAddress including both UDP and TCP ports.
        /// This constructor is used mostly internally as the offset is stored as well.
        /// </summary>
        /// <param name="inetAddress">The internet address of the peer. Can be IPv4 or IPv6.</param>
        /// <param name="tcpPort">The TCP port.</param>
        /// <param name="udpPort">The UDP port.</param>
        /// <param name="offset">The offset that we processed.</param>
        public PeerSocketAddress(IPAddress inetAddress, int tcpPort, int udpPort, long offset)
        {
            InetAddress = inetAddress;
            TcpPort = tcpPort;
            UdpPort = udpPort;
            Offset = offset;
        }

        /// <summary>
        /// Converts a byte array into a <see cref="PeerSocketAddress"/>.
        /// </summary>
        /// <param name="me">The byte array.</param>
        /// <param name="isIPv4">Whether its IPv4 or IPv6.</param>
        /// <param name="offsetOriginal">The offset from where to start reading in the array.</param>
        /// <returns>The <see cref="PeerSocketAddress"/> and the new offset.</returns>
        public static PeerSocketAddress Create(sbyte[] me, bool isIPv4, long offsetOriginal)
        {
            var offset = offsetOriginal;
            var tcpPort = ((me[offset++] & Utils.Utils.MaskFf) << Utils.Utils.ByteBits) + (me[offset++] & Utils.Utils.MaskFf);
            var udpPort = ((me[offset++] & Utils.Utils.MaskFf) << Utils.Utils.ByteBits) + (me[offset++] & Utils.Utils.MaskFf);

            IPAddress address;
            if (isIPv4)
            {
                address = Utils.Utils.Inet4AddressFromBytes(me, offset);
                offset += Utils.Utils.IPv4Bytes;
            }
            else
            {
                address = Utils.Utils.Inet6AddressFromBytes(me, offset);
                offset += Utils.Utils.IPv6Bytes;
            }
            return new PeerSocketAddress(address, tcpPort, udpPort, offset);
        }

        /// <summary>
        /// Decodes a <see cref="PeerSocketAddress"/> from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="isIPv4">Whether the address is IPv4 or IPv6.</param>
        /// <returns>The <see cref="PeerSocketAddress"/> and the new offset.</returns>
        public static PeerSocketAddress Create(JavaBinaryReader buffer, bool isIPv4)
        {
            int tcpPort = buffer.ReadUShort();
            int udpPort = buffer.ReadUShort();

            IPAddress address;
            sbyte[] me;
            if (isIPv4)
            {
                me = new sbyte[Utils.Utils.IPv4Bytes];
                buffer.ReadBytes(me);
                address = Utils.Utils.Inet4AddressFromBytes(me, 0);
            }
            else
            {
                me = new sbyte[Utils.Utils.IPv6Bytes];
                buffer.ReadBytes(me);
                address = Utils.Utils.Inet6AddressFromBytes(me, 0);
            }
            return new PeerSocketAddress(address, tcpPort, udpPort, buffer.ReaderIndex());
        }

        /// <summary>
        /// Serializes the <see cref="PeerSocketAddress"/> to a byte array.
        /// First, the ports (TCP, UDP) are serialized, then the address.
        /// </summary>
        /// <returns>The serialized <see cref="PeerSocketAddress"/>.</returns>
        public sbyte[] ToByteArray()
        {
            var size = Size();
            var result = new sbyte[size];

            var size2 = ToByteArray(result, 0);
            if (size != size2)
            {
                throw new SystemException("Sizes do not match.");
            }
            return result;
        }

        /// <summary>
        /// Serializes the <see cref="PeerSocketAddress"/> to a byte array.
        /// First, the ports (TCP, UDP) are serialized, then the address.
        /// </summary>
        /// <param name="me">The byte array to serialize to.</param>
        /// <param name="offset">The offset from where to start.</param>
        /// <returns>How many data has been written.</returns>
        public int ToByteArray(sbyte[] me, int offset)
        {
            var offset2 = offset;
            me[offset2++] = (sbyte) (TcpPort >> Utils.Utils.ByteBits); // TODO check if correct
            me[offset2++] = (sbyte) TcpPort;
            me[offset2++] = (sbyte) (UdpPort >> Utils.Utils.ByteBits);
            me[offset2++] = (sbyte) UdpPort;

            if (InetAddress.IsIPv4())
            {
                Array.Copy(InetAddress.GetAddressBytes(), 0, me, offset2, Utils.Utils.IPv4Bytes); // TODO check if works
                offset2 += Utils.Utils.IPv4Bytes;
            }
            else
            {
                Array.Copy(InetAddress.GetAddressBytes(), 0, me, offset2, Utils.Utils.IPv6Bytes); // TODO check if works
                offset2 += Utils.Utils.IPv6Bytes;
            }
            return offset2;
        }

        /// <summary>
        /// Calculates the size of this <see cref="PeerSocketAddress"/> in bytes.
        /// Format: 2 bytes TCP port, 2 bytes UDP port, 4/16 bytes IPv4/IPv6 address.
        /// </summary>
        /// <returns>The size of this <see cref="PeerSocketAddress"/> in bytes.</returns>
        public int Size()
        {
            return Size(IsIPv4);
        }

        /// <summary>
        /// Calculates the size of a <see cref="PeerSocketAddress"/> in bytes.
        /// Format: 2 bytes TCP port, 2 bytes UDP port, 4/16 bytes IPv4/IPv6 address.
        /// </summary>
        /// <param name="isIPv4">Whether the address is IPv4 or IPv6.</param>
        /// <returns>The size of this <see cref="PeerSocketAddress"/> in bytes.</returns>
        public static int Size(bool isIPv4)
        {
            return 2 + 2 + (isIPv4 ? Utils.Utils.IPv4Bytes : Utils.Utils.IPv6Bytes);
        }

        /// <summary>
        /// Creates the socket address to reach this peer with TCP.
        /// </summary>
        /// <param name="psa">The peer's <see cref="PeerSocketAddress"/>.</param>
        /// <returns>The socket address to reach this peer with TCP.</returns>
        public static IPEndPoint CreateSocketTcp(PeerSocketAddress psa)
        {
            return new IPEndPoint(psa.InetAddress, psa.TcpPort);
        }

        /// <summary>
        /// Creates the socket address to reach this peer with UDP.
        /// </summary>
        /// <param name="psa">The peer's <see cref="PeerSocketAddress"/>.</param>
        /// <returns>The socket address to reach this peer with UDP.</returns>
        public static IPEndPoint CreateSocketUdp(PeerSocketAddress psa)
        {
            return new IPEndPoint(psa.InetAddress, psa.UdpPort);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            sb.Append(InetAddress);
            if (TcpPort == UdpPort)
            {
                sb.Append(",").Append(TcpPort);
            }
            else
            {
                sb.Append(",t:").Append(TcpPort).Append(",u:").Append(UdpPort);
            }
            return sb.Append("]").ToString();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as PeerSocketAddress);
        }

        public bool Equals(PeerSocketAddress other)
        {
            var t1 = InetAddress.Equals(other.InetAddress);
            var t2 = TcpPort.Equals(other.TcpPort);
            var t3 = UdpPort.Equals(other.UdpPort);

            return t1 && t2 && t3;
        }

        public override int GetHashCode()
        {
            return InetAddress.GetHashCode() ^ TcpPort ^ UdpPort;
        }

        public bool IsIPv4
        {
            get { return InetAddress.IsIPv4(); }
        }
    }
}
