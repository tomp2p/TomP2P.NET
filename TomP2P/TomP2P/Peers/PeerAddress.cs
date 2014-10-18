using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly IPEndPoint _peerSocketAddress;

        // connection information
        private readonly bool _isNet6;
        private readonly bool _isFirewalledUdp;
        private readonly bool _isFirewalledTcp;
        private readonly bool _isRelayed;

        // calculate only once and cache
        private readonly int _hashCode; 
        // if deserialized from a byte arrays using constructor, we need to report how many data we processed
        private readonly int _offset; 

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
        private const int Mask1F = 0x1f;
        private const int Mask7 = 0x7;




        public int CompareTo(PeerAddress other)
        {
            throw new NotImplementedException();
        }
    }

}
