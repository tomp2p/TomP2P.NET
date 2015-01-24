using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TomP2P.Extensions;

namespace TomP2P.Connection
{
    /// <summary>
    /// Gathers information about interface bindings.
    /// Here, a user can set the preferences to which addresses to bind the socket.
    /// This class contains two types of information:
    /// 1. The interface/address to listen for incoming connections
    /// 2. How other peers see us
    /// The default is to listen to all interfaces and our outside address is set
    /// to the first interface it finds. If more than one search hint is used, the
    /// combination operation will be "and".
    /// </summary>
    public class Bindings
    {
        // this can be set by the user
        // the discover process will not use this field to store anything
        private readonly IList<IPAddress> _addresses = new List<IPAddress>(1);
        private readonly IList<string> _interfaceHints = new List<string>(1);
        private readonly IList<AddressFamily> _protocolHint = new List<AddressFamily>(1);

        // here are the found addresses stored (not set by the user)
        private readonly List<IPAddress> _foundBroadcastAddresses = new List<IPAddress>(1);
        private readonly List<IPAddress> _foundAddresses4 = new List<IPAddress>(1);
        private readonly List<IPAddress> _foundAddresses6 = new List<IPAddress>(1);

        internal Bindings AddFoundAddress(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentException("Cannot add null.");
            }
            if (address.IsIPv4())
            {
                _foundAddresses4.Add(address);
            }
            else if (address.IsIPv6())
            {
                _foundAddresses6.Add(address);
            }
            else
            {
                throw new ArgumentException(String.Format("Unknown address family {0}.", address.AddressFamily));
            }
            return this;
        }

        internal bool ContainsAddress(IPAddress address)
        {
            return _foundAddresses4.Contains(address) || _foundAddresses6.Contains(address);
        }

        /// <summary>
        /// Returns a list of IPAddresses to listen to. First IPv4 addresses, then IPv6 addresses are present in the list.
        /// </summary>
        public IList<IPAddress> FoundAddresses
        {
            get
            {
                // first return IPv4, then IPv6
                var listenAddresses = new List<IPAddress>(_foundAddresses4.Count + _foundAddresses6.Count);
                listenAddresses.AddRange(_foundAddresses4);
                listenAddresses.AddRange(_foundAddresses6);
                return listenAddresses;
            }
        }

        public IPAddress FoundAddress
        {
            get
            {
                return FoundAddresses.Count == 0 ? null : FoundAddresses[0];
            }
        }

        /// <summary>
        /// A list of broadcast addresses.
        /// </summary>
        public IList<IPAddress> BroadcastAddresses
        {
            get { return _foundBroadcastAddresses; }
        }

        /// <summary>
        /// Adds an address that we want to listen to. If the address is not found, it will be ignored.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public Bindings AddAddress(IPAddress address)
        {
            _addresses.Add(address);
            return this;
        }

        /// <summary>
        /// A list of the addresses provided by the user.
        /// </summary>
        public IList<IPAddress> Addresses
        {
            get { return _addresses; }
        }

        public Bindings AddInterface(string interfaceHint)
        {
            if (interfaceHint == null)
            {
                throw new ArgumentException("Cannot add null.");
            }
            _interfaceHints.Add(interfaceHint);
            return this;
        }

        public Bindings AddProtocol(AddressFamily protocolFamily)
        {
            _protocolHint.Add(protocolFamily);
            return this;
        }

        /// <summary>
        /// A list of interfaces to listen to.
        /// </summary>
        public IList<string> InterfaceHints
        {
            get { return _interfaceHints; }
        }

        /// <summary>
        /// The protocol to listen to.
        /// </summary>
        public IList<AddressFamily> ProtocolHint
        {
            get { return _protocolHint; }
        }

        /// <summary>
        /// Clears all lists.
        /// </summary>
        public void Clear()
        {
            _interfaceHints.Clear();
            _addresses.Clear();
            _foundAddresses4.Clear();
            _foundAddresses6.Clear();
            _foundBroadcastAddresses.Clear();
        }

        /// <summary>
        /// Checks if the user sets any addresses.
        /// </summary>
        public bool AnyAddresses
        {
            get { return _addresses.Count == 0; }
        }

        /// <summary>
        /// Checks if the user sets any interfaces.
        /// </summary>
        public bool AnyInterfaces
        {
            get { return _interfaceHints.Count == 0; }
        }

        /// <summary>
        /// Checks if the user sets any protocols.
        /// </summary>
        public bool AnyProtocols
        {
            get { return _protocolHint.Count == 0; }
        }

        /// <summary>
        /// Checks if the user sets protocol to anything or IPv4.
        /// </summary>
        public bool IsIPv4
        {
            get { return AnyProtocols || _protocolHint.Contains(AddressFamily.InterNetwork); }
        }

        /// <summary>
        /// Checks if the user sets protocol to anything or IPv6.
        /// </summary>
        public bool IsIPv6
        {
            get { return AnyProtocols || _protocolHint.Contains(AddressFamily.InterNetworkV6); }
        }

        /// <summary>
        /// Checks if the user sets anything at all.
        /// </summary>
        public bool IsListenAll
        {
            get { return AnyProtocols && AnyInterfaces && AnyAddresses; }
        }

        /// <summary>
        /// Checks if the user provided an interface hint.
        /// </summary>
        /// <param name="name">The name of the interface reported by the system.</param>
        /// <returns>True, if the user added the interface.</returns>
        public bool ContainsInterface(string name)
        {
            return _interfaceHints.Contains(name);
        }

        /// <summary>
        /// Adds the results from another binding. This is useful because you can
        /// add within one bindings hintrs only with "and". With Add(), you have
        /// the option "or" as well. E.g., Bindings b1 = new Bindings(IPv4, eth0);
        /// Bindings b2 = new Bindings(IPv6, eth1); b2.add(b1) -> this will bind to
        /// all IPv4 addresses on eth0 and all IPv6 addresses on eth1.
        /// </summary>
        /// <param name="other">The other instance to get the results from.</param>
        /// <returns>This class.</returns>
        public Bindings Add(Bindings other)
        {
            _foundAddresses4.AddRange(other._foundAddresses4);
            _foundAddresses6.AddRange(other._foundAddresses6);
            _foundBroadcastAddresses.AddRange(other._foundBroadcastAddresses);
            return this;
        }

        public IPEndPoint WildcardSocket()
        {
            if (!IsListenAll)
            {
                if (_foundAddresses4.Count > 0)
                {
                    return new IPEndPoint(_foundAddresses4[0], 0);
                }
                if (_foundAddresses6.Count > 0)
                {
                    return new IPEndPoint(_foundAddresses6[0], 0);
                }
            }
            return new IPEndPoint(IPAddress.Any, 0);
        }
    }
}
