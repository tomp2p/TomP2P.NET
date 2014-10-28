using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using TomP2P.Workaround;

namespace TomP2P.Peers
{
    /// <summary>
    /// A peer address contains the node ID and how to contact this node both TCP and UDP.
    /// <para>The following format applies:</para>
    /// <para>20 bytes      Number160</para>
    /// <para> 2 bytes      Header</para>
    /// <para>  - 1 byte Options: IPv6, firewalled UDP, firewalled TCP</para>
    /// <para>  - 1 byte Relays:</para>
    /// <para>      - first 3 bits: nr of relays (max. 5)</para>
    /// <para>      - last  5 bits: if the 5 relays are IPv6 (bit set) or not (no bit set)</para>
    /// <para> 2 bytes      TCP port</para>
    /// <para> 2 bytes      UDP port</para>
    /// <para> 2 or 4bytes  Inet Address</para>
    /// <para> 0-5 relays:</para>
    /// <para>  - 2 bytes       TCP port</para>
    /// <para>  - 2 bytes       UDP port</para>
    /// <para>  - 4 or 16 bytes Inet Address</para>
    /// </summary>
    public class PeerAddress : IComparable<PeerAddress>, IEquatable<PeerAddress>
    {
        public const int MaxSize = 142;
        public const int MinSize = 30;
        public const int MaxRelays = 5;

        private const int Net6 = 1;         // 0001
        private const int FirewallUdp = 2;  // 0010
        private const int FirewallTcp = 4;  // 0100
        private const int Relayed = 8;      // 1000

        // network information
        /// <summary>
        /// The ID of the peer. A peer cannot change its ID.
        /// </summary>
        public Number160 PeerId { get; private set; }
        public PeerSocketAddress PeerSocketAddress { get; private set; }

        // connection information
        /// <summary>
        /// True, if the internet address is IPv6.
        /// </summary>
        public bool IsIPv6 { get; private set; }
        /// <summary>
        /// True, if the internet address is IPv4.
        /// </summary>
        public bool IsIPv4 { get { return !IsIPv6; } }

        /// <summary>
        /// True, if the peer cannot be reached via UDP.
        /// </summary>
        public bool IsFirewalledUdp { get; private set; }

        /// <summary>
        /// True, if the peer cannot be reached via TCP.
        /// </summary>
        public bool IsFirewalledTcp { get; private set; }

        /// <summary>
        /// True, if the peer address is used as a relay.
        /// </summary>
        public bool IsRelayed { get; private set; }

        // calculate only once and cache
        private readonly int _hashCode; 

        /// <summary>
        /// When deserializing, we need to know how much we deserialized from the constructor call.
        /// </summary>
        public long Offset { get; private set; } 

        /// <summary>
        /// The size of the serialized peer address.
        /// </summary>
        public long Size { get; private set; }

        public int RelaySize { get; private set; }
        private readonly BitArray _relayType;
        private static readonly BitArray EmptyRelayType = new BitArray(0);

        // relays
        /// <summary>
        /// The relay peers.
        /// </summary>
        public ICollection<PeerSocketAddress> PeerSocketAddresses { get; private set; }
        public static readonly ICollection<PeerSocketAddress> EmptyPeerSocketAddresses = new HashSet<PeerSocketAddress>();

        private const int TypeBitSize = 5;
        private const int HeaderSize = 2;
        private const int PortsSize = 4; // count both ports, UDP and TCP

        // used for relay bit shifting
        private const int Mask1F = 0x1f;    // 0001 1111
        private const int Mask07 = 0x7;      // 0000 0111

        /// <summary>
        /// Creates a peer address where the byte array has to be in the right format and size.
        /// The new offset can be accessed through the Offset property.
        /// </summary>
        /// <param name="me">The serialized array.</param>
        public PeerAddress(sbyte[] me)
            : this(me, 0)
        { }

        /// <summary>
        /// Creates a peer address from a continuous byte array. This is useful if you don't know the size beforehand.
        /// The new offset can be accessed through the Offset property.
        /// </summary>
        /// <param name="me">The serialized array.</param>
        /// <param name="initialOffset">The offset where to start.</param>
        public PeerAddress(sbyte[] me, int initialOffset)
        {
            // get the peer ID, this is independent of the type
            long offset = initialOffset;

            // get the options
            int options = me[offset++] & Utils.Utils.MaskFf;
            IsIPv6 = (options & Net6) > 0; // TODO static methods could be used instead
            IsFirewalledUdp = (options & FirewallUdp) > 0;
            IsFirewalledTcp = (options & FirewallTcp) > 0;
            IsRelayed = (options & Relayed) > 0;

            // get the relays
            int relays = me[offset++] & Utils.Utils.MaskFf;

            // first 3 bits are the size
            RelaySize = (relays >> TypeBitSize) & Mask07;
            
            // last 5 bits indicate if IPv6 or IPv4
            var b = (byte)(relays & Mask1F); // TODO check if works (2x)
            _relayType = new BitArray(b);

            // get the ID
            var tmp = new sbyte[Number160.ByteArraySize];
            Array.Copy(me, offset, tmp, 0, Number160.ByteArraySize);
            PeerId = new Number160(tmp);
            Offset += Number160.ByteArraySize;

            PeerSocketAddress = PeerSocketAddress.Create(me, IsIPv4, offset);
            offset = PeerSocketAddress.Offset;
            if (RelaySize > 0)
            {
                PeerSocketAddresses = new List<PeerSocketAddress>(RelaySize);
                for (int i = 0; i < RelaySize; i++)
                {
                    var psa = PeerSocketAddress.Create(me, !_relayType.Get(i), offset);
                    PeerSocketAddresses.Add(psa);
                    offset = psa.Offset;
                }
            }
            else
            {
                PeerSocketAddresses = EmptyPeerSocketAddresses;
            }

            Size = offset - initialOffset;
            Offset = offset;
            _hashCode = PeerId.GetHashCode();
        }

        /// <summary>
        /// Creates a peer address from a byte buffer.
        /// </summary>
        /// <param name="buffer">The channel buffer to read from.</param>
        public PeerAddress(JavaBinaryReader buffer)
        {
            long readerIndex = buffer.ReaderIndex;

            // get the type
            int options = buffer.ReadByte();
            IsIPv6 = (options & Net6) > 0;
            IsFirewalledUdp = (options & FirewallUdp) > 0;
            IsFirewalledTcp = (options & FirewallTcp) > 0;
            IsRelayed = (options & Relayed) > 0;

            // get the relays
            int relays = buffer.ReadByte();
            RelaySize = (relays >> TypeBitSize) & Mask07;
            var b = (byte) (relays & Mask1F); // TODO check if works (2x)
            _relayType = new BitArray(b);

            // get the ID
            var me = new sbyte[Number160.ByteArraySize];
            buffer.ReadBytes(me);
            PeerId = new Number160(me);

            PeerSocketAddress = PeerSocketAddress.Create(buffer, IsIPv4);

            if (RelaySize > 0)
            {
                PeerSocketAddresses = new List<PeerSocketAddress>(RelaySize);
                for (int i = 0; i < RelaySize; i++)
                {
                    PeerSocketAddresses.Add(PeerSocketAddress.Create(buffer, !_relayType.Get(i)));
                }
            }
            else
            {
                PeerSocketAddresses = EmptyPeerSocketAddresses;
            }

            Size = buffer.ReaderIndex - readerIndex;

            Offset = -1; // not used here
            _hashCode = PeerId.GetHashCode();
        }

        /// <summary>
        /// If you only need to know the ID.
        /// </summary>
        /// <param name="id">The ID of the peer.</param>
        public PeerAddress(Number160 id)
            : this(id, (IPAddress) null, -1, -1)
        { }

        /// <summary>
        /// If you only need to know the ID and the internet address.
        /// </summary>
        /// <param name="id">The ID of the peer.</param>
        /// <param name="inetAddress">The internet address of the peer.</param>
        public PeerAddress(Number160 id, IPAddress inetAddress)
            : this(id, inetAddress, -1, -1)
        { }

        /// <summary>
        /// Creates a peer address if all the values are known.
        /// </summary>
        /// <param name="id">The ID of the peer.</param>
        /// <param name="peerSocketAddress">The peer socket address including both ports UDP and TCP.</param>
        /// <param name="isFirewalledTcp">Indicates if the peer is not reachable via UDP.</param>
        /// <param name="isFirewalledUdp">Indicates if the peer is not reachable via TCP.</param>
        /// <param name="isRelayed">Indicates if the peer is used as a relay.</param>
        /// <param name="peerSocketAddresses">The relay peers.</param>
        public PeerAddress(Number160 id, PeerSocketAddress peerSocketAddress, bool isFirewalledTcp, bool isFirewalledUdp,
            bool isRelayed, ICollection<PeerSocketAddress> peerSocketAddresses)
        {
            PeerId = id;
            int size = Number160.ByteArraySize;
            PeerSocketAddress = peerSocketAddress;
            _hashCode = id.GetHashCode();
            IsIPv6 = false; // TODO implement correctly
            IsFirewalledUdp = isFirewalledUdp;
            IsFirewalledTcp = isFirewalledTcp;
            IsRelayed = isRelayed;

            // header + TCP port + UDP port
            size += HeaderSize + PortsSize + (IsIPv6 ? Utils.Utils.IPv6Bytes : Utils.Utils.IPv4Bytes);

            if (PeerSocketAddresses == null)
            {
                PeerSocketAddresses = EmptyPeerSocketAddresses;
                _relayType = EmptyRelayType;
                RelaySize = 0;
            }
            else
            {
                RelaySize = PeerSocketAddresses.Count;
                if (RelaySize > TypeBitSize)
                {
                    throw new ArgumentException(String.Format("Can only store up to {0} relay peers. Tried to store {1} relay peers.", TypeBitSize, RelaySize));
                }
                PeerSocketAddresses = peerSocketAddresses;
                _relayType = new BitArray(RelaySize);
            }
            int index = 0;
            foreach (var psa in peerSocketAddresses)
            {
                bool isIPv6 = false; // TODO implement correctly
                _relayType.Set(index, isIPv6);
                size += psa.Size();
                index++;
            }
            Size = size;
            Offset = -1; // not used here
        }

        // Facade Constructors:
        // TODO document

        public PeerAddress(Number160 peerId, IPAddress inetAddress, int tcpPort, int udpPort)
            : this(peerId, new PeerSocketAddress(inetAddress, tcpPort, udpPort), false, false, false, EmptyPeerSocketAddresses)
        { }

        // TODO exception handling from invalid string format
        public PeerAddress(Number160 peerId, string address, int tcpPort, int udpPort)
            : this(peerId, IPAddress.Parse(address), tcpPort, udpPort)
        { }

        public PeerAddress(Number160 peerId, IPEndPoint inetSocketAddress)
            : this(peerId, inetSocketAddress.Address, inetSocketAddress.Port, inetSocketAddress.Port)
        { }

        public PeerAddress(Number160 peerId, IPAddress inetAddress, int tcpPort, int udpPort, int options)
            : this(peerId, new PeerSocketAddress(inetAddress, tcpPort, udpPort), ReadIsFirewalledTcp(options), ReadIsFirewalledUdp(options), ReadIsRelay(options), EmptyPeerSocketAddresses)
        { }

        /// <summary>
        /// Serializes to a new array with the proper size.
        /// </summary>
        /// <returns>The serialized representation.</returns>
        public sbyte[] ToByteArray()
        {
            var me = new sbyte[Size];
            ToByteArray(me, 0); // TODO check if references are updated
            return me;
        }

        /// <summary>
        /// Serializes to an existing array.
        /// </summary>
        /// <param name="me">The array where the result should be stored.</param>
        /// <param name="offset">The offset where to start to save the result in the byte array.</param>
        /// <returns>The new offset.</returns>
        public int ToByteArray(sbyte[] me, int offset)
        {
            // save the peer ID
            int newOffset = offset;
            me[newOffset++] = Options;
            me[newOffset++] = Relays;
            newOffset = PeerId.ToByteArray(me, newOffset);

            // we store both the addresses of the peer and the relays
            // currently, this is not needed as we don't consider asymmetric relays
            newOffset = PeerSocketAddress.ToByteArray(me, newOffset);

            foreach (var psa in PeerSocketAddresses)
            {
                newOffset = psa.ToByteArray(me, newOffset);
            }

            return newOffset;
        }

        /// <summary>
        /// Creates and returns the socket address using the TCP port.
        /// </summary>
        /// <returns>The socket address how to reach this peer.</returns>
        public IPEndPoint CreateSocketTcp()
        {
            return new IPEndPoint(PeerSocketAddress.InetAddress, PeerSocketAddress.TcpPort);
        }

        /// <summary>
        /// Creates and returns the socket address using the UDP port.
        /// </summary>
        /// <returns>The socket address how to reach this peer.</returns>
        public IPEndPoint CreateSocketUdp()
        {
            return new IPEndPoint(PeerSocketAddress.InetAddress, PeerSocketAddress.UdpPort);
        }

        /// <summary>
        /// Create a new peer address and change the relayed status.
        /// </summary>
        /// <param name="isRelayed">The new relay status.</param>
        /// <returns>The newly created peer address.</returns>
        public PeerAddress ChangeIsRelayed(bool isRelayed)
        {
            return new PeerAddress(PeerId, PeerSocketAddress, IsFirewalledTcp, IsFirewalledUdp, isRelayed, PeerSocketAddresses);
        }

        /// <summary>
        /// Create a new peer address and change the firewall UDP status.
        /// </summary>
        /// <param name="isFirewalledUdp">The new firewall UDP status.</param>
        /// <returns>The newly created peer address.</returns>
        public PeerAddress ChangeIsFirewalledUdp(bool isFirewalledUdp)
        {
            return new PeerAddress(PeerId, PeerSocketAddress, IsFirewalledTcp, isFirewalledUdp, IsRelayed, PeerSocketAddresses);
        }

        /// <summary>
        /// Create a new peer address and change the firewall TCP status.
        /// </summary>
        /// <param name="isFirewalledTcp">The new firewall TCP status.</param>
        /// <returns>The newly created peer address.</returns>
        public PeerAddress ChangeIsFirewalledTcp(bool isFirewalledTcp)
        {
            return new PeerAddress(PeerId, PeerSocketAddress, isFirewalledTcp, IsFirewalledUdp, IsRelayed, PeerSocketAddresses);
        }

        /// <summary>
        /// Create a new peer address and change the internet address.
        /// </summary>
        /// <param name="inetAddress">The new internet address.</param>
        /// <returns>The newly created peer address.</returns>
        public PeerAddress ChangeAddress(IPAddress inetAddress)
        {
            return new PeerAddress(PeerId, new PeerSocketAddress(inetAddress, PeerSocketAddress.TcpPort, PeerSocketAddress.UdpPort), IsFirewalledTcp, IsFirewalledUdp, IsRelayed, PeerSocketAddresses);
        }

        /// <summary>
        /// Create a new peer address and change the TCP and UDP ports.
        /// </summary>
        /// <param name="tcpPort">The new TCP port.</param>
        /// <param name="udpPort">The new UDP port.</param>
        /// <returns>The newly created peer address.</returns>
        public PeerAddress ChangePorts(int tcpPort, int udpPort)
        {
            return new PeerAddress(PeerId, new PeerSocketAddress(PeerSocketAddress.InetAddress, tcpPort, udpPort), IsFirewalledTcp, IsFirewalledUdp, IsRelayed, PeerSocketAddresses);
        }

        /// <summary>
        /// Create a new peer address and change the peer ID.
        /// </summary>
        /// <param name="peerId">The new peer ID.</param>
        /// <returns>The newly created peer address.</returns>
        public PeerAddress ChangePeerId(Number160 peerId)
        {
            return new PeerAddress(peerId, PeerSocketAddress, IsFirewalledTcp, IsFirewalledUdp, IsRelayed, PeerSocketAddresses);
        }

        public PeerAddress ChangePeerSocketAddress(PeerSocketAddress peerSocketAddress)
        {
            return new PeerAddress(PeerId, peerSocketAddress, IsFirewalledTcp, IsFirewalledUdp, IsRelayed, PeerSocketAddresses);
        }

        public PeerAddress ChangePeerSocketAddresses(ICollection<PeerSocketAddress> peerSocketAddresses)
        {
            return new PeerAddress(PeerId, PeerSocketAddress, IsFirewalledTcp, IsFirewalledUdp, IsRelayed, peerSocketAddresses);   
        }

        /// <summary>
        /// Calculates the size based on the two header bytes.
        /// </summary>
        /// <param name="header">The header in the lower 16 bits of this integer.</param>
        /// <returns>The expected size of the peer address.</returns>
        public static int CalculateSize(int header)
        {
            // TODO check correctness, potential BUG
            int options = (header >> Utils.Utils.ByteBits) & Utils.Utils.MaskFf;
            int relays = header & Utils.Utils.MaskFf;

            return CalculateSize(options, relays);
        }

        /// <summary>
        /// Calculates the size based on the two header bytes.
        /// </summary>
        /// <param name="options">The options tell us if the internet address is IPv4 or IPv6.</param>
        /// <param name="relays">The relays tell us how many relays we have and of what type they are.</param>
        /// <returns>The expected size of the peer address.</returns>
        public static int CalculateSize(int options, int relays)
        {
            // header + tcp port + udp port + peer id
            int size = HeaderSize + PortsSize + Number160.ByteArraySize;

            if (ReadIsNet6(options))
            {
                size += Utils.Utils.IPv6Bytes;
            }
            else
            {
                size += Utils.Utils.IPv4Bytes;
            }

            // count the relays
            int relaySize = (relays >> TypeBitSize) & Mask07;
            var b = (byte) (relays & Mask1F);
            var relayType = new BitArray(b);
            for (int i = 0; i < relaySize; i++)
            {
                size += PortsSize;
                if (relayType.Get(i))
                {
                    size += Utils.Utils.IPv6Bytes;
                }
                else
                {
                    size += Utils.Utils.IPv4Bytes;
                }
            }
            return size;
        }

        /// <summary>
        /// Checks if options has IPv6 set.
        /// </summary>
        /// <param name="options">The option field, lowest 8 bit.</param>
        /// <returns>True, if its IPv6.</returns>
        private static bool ReadIsNet6(int options)
        {
            return ((options & Utils.Utils.MaskFf) & Net6) > 0;
        }

        /// <summary>
        /// Checks if options has firewall UDP set.
        /// </summary>
        /// <param name="options">The option field, lowest 8 bit.</param>
        /// <returns>True, if its firewalled via UDP.</returns>
        private static bool ReadIsFirewalledUdp(int options)
        {
            return ((options & Utils.Utils.MaskFf) & FirewallUdp) > 0;
        }

        /// <summary>
        /// Checks if options has firewall TCP set.
        /// </summary>
        /// <param name="options">The option field, lowest 8 bit.</param>
        /// <returns>True, if its firewalled via TCP.</returns>
        private static bool ReadIsFirewalledTcp(int options)
        {
            return ((options & Utils.Utils.MaskFf) & FirewallTcp) > 0;
        }

        /// <summary>
        /// Checks if options has relay flag set.
        /// </summary>
        /// <param name="options">The option field, lowest 8 bit.</param>
        /// <returns>True, if its used as a relay.</returns>
        private static bool ReadIsRelay(int options)
        {
            return ((options & Utils.Utils.MaskFf) & Relayed) > 0;
        }

        public int CompareTo(PeerAddress other)
        {
            // the ID determines if two peers are equal, the address does not matter
            return PeerId.CompareTo(other.PeerId);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("paddr[");
            return sb.Append(PeerId)
                .Append(PeerSocketAddress)
                .Append("]/relay(").Append(IsRelayed)
                .Append(",").Append(PeerSocketAddresses.Count)
                .Append(")=").Append(PeerSocketAddresses.ToArray())
                .ToString();
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
            return Equals(obj as PeerAddress);
        }

        public bool Equals(PeerAddress other)
        {
            return PeerId.Equals(other.PeerId);
        }

        public override int GetHashCode()
        {
            // use cached hash code
            return _hashCode;
        }

        /// <summary>
        /// The IP address of this peer.
        /// </summary>
        public IPAddress InetAddress
        {
            get { return PeerSocketAddress.InetAddress; }
        }

        /// <summary>
        /// The encoded options.
        /// </summary>
        public sbyte Options
        {
            get
            {
                sbyte result = 0;
                if (IsIPv6)
                {
                    result |= Net6;
                }
                if (IsFirewalledUdp)
                {
                    result |= FirewallUdp;
                }
                if (IsFirewalledTcp)
                {
                    result |= FirewallTcp;
                }
                if (IsRelayed)
                {
                    result |= Relayed;
                }
                return result;
            }
        }

        /// <summary>
        /// The encoded relays. There are maximal 5 relays.
        /// </summary>
        public sbyte Relays
        {
            get
            {
                if (RelaySize > 0)
                {
                    var result = (sbyte) (RelaySize << TypeBitSize);
                    sbyte types = _relayType.ToByte();
                    result |= (sbyte) (types & Mask1F);
                    return result;
                }
                return 0;
            }
        }

        /// <summary>
        /// UDP port.
        /// </summary>
        public int UdpPort
        {
            get { return PeerSocketAddress.UdpPort; }
        }

        /// <summary>
        /// TCP port.
        /// </summary>
        public int TcpPort
        {
            get { return PeerSocketAddress.TcpPort; }
        }
    }

}
