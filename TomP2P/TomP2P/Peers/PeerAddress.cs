using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
    public class PeerAddress : IComparable<PeerAddress>
    {
        public const int MaxSize = 142;
        public const int MinSize = 30;
        public const int MaxRelays = 5;

        private const int Net6 = 1;
        private const int FirewallUdp = 2;
        private const int FirewallTcp = 4;
        private const int IsRelayed = 8;

        // network information
        public Number160 PeerId { get; private set; }
        private readonly PeerSocketAddress _peerSocketAddress;

        // connection information
        private readonly bool _isNet6;
        private readonly bool _isFirewalledUdp;
        private readonly bool _isFirewalledTcp;
        private readonly bool _isRelayed;

        // calculate only once and cache
        private readonly int _hashCode; 

        /// <summary>
        /// When deserializing, we need to know how much we deserialized from the constructor call.
        /// </summary>
        public int Offset { get; private set; } 

        /// <summary>
        /// The size of the serialized peer address.
        /// </summary>
        public int Size { get; private set; }

        private readonly int _relaySize;
        private readonly BitArray _relayType;
        private static readonly BitArray EmptyRelayType = new BitArray(0);

        // relays
        private readonly ICollection<PeerSocketAddress> _peerSocketAddresses;
        public static readonly ICollection<PeerSocketAddress> EmptyPeerSocketAddresses = new HashSet<PeerSocketAddress>();

        private const int TypeBitSize = 5;
        private const int HeaderSize = 2;
        private const int PortSize = 4; // count both ports, UDP and TCP

        // used for relay bit shifting
        private const int Mask1F = 0x1f;    // 0001 1111
        private const int Mask7 = 0x7;      // 0000 0111

        /// <summary>
        /// Creates a peer address where the byte array has to be in the right format and size.
        /// The new offset can be accessed through the Offset property.
        /// </summary>
        /// <param name="me">The serialized array.</param>
        public PeerAddress(byte[] me)
            : this(me, 0)
        { }

        /// <summary>
        /// Creates a peer address from a continuous byte array. This is useful if you don't know the size beforehand.
        /// The new offset can be accessed through the Offset property.
        /// </summary>
        /// <param name="me">The serialized array.</param>
        /// <param name="initialOffset">The offset where to start.</param>
        public PeerAddress(byte[] me, int initialOffset)
        {
            // get the peer ID, this is independent of the type
            int offset = initialOffset;

            // get the options
            int options = me[offset++] & Utils.Utils.MaskFf;
            _isNet6 = (options & Net6) > 0; // TODO static methods could be used instead
            _isFirewalledUdp = (options & FirewallUdp) > 0;
            _isFirewalledTcp = (options & FirewallTcp) > 0;
            _isRelayed = (options & IsRelayed) > 0;

            // get the relays
            int relays = me[offset++] & Utils.Utils.MaskFf;

            // first 3 bits are the size
            _relaySize = (relays >> TypeBitSize) & Mask7;
            
            // last 5 bits indicate if IPv6 or IPv4
            var b = (byte)(relays & Mask1F);
            _relayType = new BitArray(b);

            // get the ID
            var tmp = new byte[Number160.ByteArraySize];
            Array.Copy(me, offset, tmp, 0, Number160.ByteArraySize);
            PeerId = new Number160(tmp);
            Offset += Number160.ByteArraySize;

            _peerSocketAddress = PeerSocketAddress.Create(me, IsIPv4, offset);
            offset = _peerSocketAddress.Offset;
            if (_relaySize > 0)
            {
                _peerSocketAddresses = new List<PeerSocketAddress>(_relaySize);
                for (int i = 0; i < _relaySize; i++)
                {
                    var psa = PeerSocketAddress.Create(me, !_relayType.Get(i), offset);
                    _peerSocketAddresses.Add(psa);
                    offset = psa.Offset;
                }
            }
            else
            {
                _peerSocketAddresses = EmptyPeerSocketAddresses;
            }

            Size = offset - initialOffset;
            Offset = offset;
            _hashCode = PeerId.GetHashCode();
        }

        /// <summary>
        /// Creates a peer address from a byte buffer.
        /// </summary>
        /// <param name="channelBuffer">The channel buffer to read from.</param>
        public PeerAddress(MemoryStream channelBuffer)
        {
            // TODO implement
            throw new NotImplementedException();
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
            _peerSocketAddress = peerSocketAddress;
            _hashCode = id.GetHashCode();
            _isNet6 = false; // TODO implement correctly
            _isFirewalledUdp = isFirewalledUdp;
            _isFirewalledTcp = isFirewalledTcp;
            _isRelayed = isRelayed;

            // header + TCP port + UDP port
            size += HeaderSize + PortSize + (_isNet6 ? Utils.Utils.IPv6Bytes : Utils.Utils.IPv4Bytes);

            if (_peerSocketAddresses == null)
            {
                _peerSocketAddresses = EmptyPeerSocketAddresses;
                _relayType = EmptyRelayType;
                _relaySize = 0;
            }
            else
            {
                _relaySize = _peerSocketAddresses.Count;
                if (_relaySize > TypeBitSize)
                {
                    throw new ArgumentException(String.Format("Can only store up to {0} relay peers. Tried to store {1} relay peers.", TypeBitSize, _relaySize));
                }
                _peerSocketAddresses = peerSocketAddresses;
                _relayType = new BitArray(_relaySize);
            }
            int index = 0;
            foreach (var psa in peerSocketAddresses)
            {
                bool isIPv6 = false; // TODO implement correctly
                _relayType.Set(index, isIPv6);
                size += psa.Size;
                index++;
            }
            Size = size;
            Offset = -1; // unused here
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
            : this(peerId, new PeerSocketAddress(inetAddress, tcpPort, udpPort), IsFirewalledTCP(options), IsFirewalledUDP(options), IsRelay(options), EmptyPeerSocketAddresses)
        { }

        /// <summary>
        /// Checks if options has firewall TCP set.
        /// </summary>
        /// <param name="options">The option field, lowest 8 bit.</param>
        /// <returns>True, if its firewalled via TCP.</returns>
        private static bool IsFirewalledTCP(int options)
        {
            return ((options & Utils.Utils.MaskFf) & FirewallTcp) > 0;
        }

        /// <summary>
        /// Checks if options has IPv6 set.
        /// </summary>
        /// <param name="options">The option field, lowest 8 bit.</param>
        /// <returns>True, if its IPv6.</returns>
        private static bool IsNet6(int options)
        {
            return ((options & Utils.Utils.MaskFf) & Net6) > 0;
        }

        /// <summary>
        /// Checks if options has firewall UDP set.
        /// </summary>
        /// <param name="options">The option field, lowest 8 bit.</param>
        /// <returns>True, if its firewalled via UDP.</returns>
        private static bool IsFirewalledUDP(int options)
        {
            return ((options & Utils.Utils.MaskFf) & FirewallUdp) > 0;
        }

        /// <summary>
        /// Checks if options has relay flag set.
        /// </summary>
        /// <param name="options">The option field, lowest 8 bit.</param>
        /// <returns>True, if its used as a relay.</returns>
        private static bool IsRelay(int options)
        {
            return ((options & Utils.Utils.MaskFf) & IsRelayed) > 0;
        }

        public int CompareTo(PeerAddress other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True, if the Inet address is IPv4.
        /// </summary>
        public bool IsIPv4
        {
            get { return !_isNet6; }
        }
    }

}
