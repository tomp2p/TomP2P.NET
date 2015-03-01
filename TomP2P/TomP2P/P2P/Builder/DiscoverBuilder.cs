using System;
using System.Linq;
using System.Net;
using NLog;
using TomP2P.Connection;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;
using TomP2P.Futures;
using TomP2P.Peers;

namespace TomP2P.P2P.Builder
{
    public class DiscoverBuilder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly TcsDiscover TcsDiscoverShutdown = new TcsDiscover();

        private readonly Peer _peer;
        public IPAddress InetAddress { get; private set; }
        public int PortUdp { get; private set; }
        public int PortTcp { get; private set; }
        public PeerAddress PeerAddress { get; private set; }
        public PeerAddress SenderAddress { get; private set; }

        public int DiscoverTimeoutSec { get; private set; }

        private IConnectionConfiguration _configuration;

        public TcsDiscover TcsDiscover { get; private set; }

        // static constructor
        static DiscoverBuilder()
        {
            TcsDiscoverShutdown.SetException(new TaskFailedException("Peer is shutting down."));
        }

        public DiscoverBuilder(Peer peer)
        {
            _peer = peer;
            PortUdp = Ports.DefaultPort;
            PortTcp = Ports.DefaultPort;
            DiscoverTimeoutSec = 5;
        }

        public DiscoverBuilder SetInetAddress(IPAddress inetAddress)
        {
            InetAddress = inetAddress;
            return this;
        }

        public DiscoverBuilder SetInetAddress(IPAddress inetAddress, int port)
        {
            InetAddress = inetAddress;
            PortTcp = port;
            PortUdp = port;
            return this;
        }

        public DiscoverBuilder SetInetAddress(IPAddress inetAddress, int portTcp, int portUdp)
        {
            InetAddress = inetAddress;
            PortTcp = portTcp;
            PortUdp = portUdp;
            return this;
        }

        public DiscoverBuilder SetPortTcp(int portTcp)
        {
            PortTcp = portTcp;
            return this;
        }

        public DiscoverBuilder SetPortUdp(int portUdp)
        {
            PortUdp = portUdp;
            return this;
        }

        public DiscoverBuilder SetPorts(int port)
        {
            PortTcp = port;
            PortUdp = port;
            return this;
        }

        public DiscoverBuilder SetPeerAddress(PeerAddress peerAddress)
        {
            PeerAddress = peerAddress;
            return this;
        }

        public DiscoverBuilder SetSenderAddress(PeerAddress senderAddress)
        {
            SenderAddress = senderAddress;
            return this;
        }

        public DiscoverBuilder SetDiscoverTimeoutSec(int discoverTimeoutSec)
        {
            DiscoverTimeoutSec = discoverTimeoutSec;
            return this;
        }

        public DiscoverBuilder SetTcsDiscover(TcsDiscover tcsDiscover)
        {
            TcsDiscover = tcsDiscover;
            return this;
        }

        public TcsDiscover Start()
        {
            if (_peer.IsShutdown)
            {
                return TcsDiscoverShutdown;
            }

            if (PeerAddress == null && InetAddress != null)
            {
                PeerAddress = new PeerAddress(Number160.Zero, InetAddress, PortTcp, PortUdp);
            }
            if (PeerAddress == null)
            {
                throw new ArgumentException("Peer address or inet address required.");
            }
            if (_configuration == null)
            {
                _configuration = new DefaultConnectionConfiguration();
            }
            if (TcsDiscover == null)
            {
                TcsDiscover = new TcsDiscover();
            }

            return Discover(PeerAddress, _configuration, TcsDiscover);
        }

        /// <summary>
        /// Discover attempts to find the external IP address of this peer. This is done by first trying to set
        /// UPNP with port forwarding (gives us the external address), query UPNP for the external address, and
        /// pinging a well-known peer. The fallback is NAT-PMP.
        /// </summary>
        /// <param name="peerAddress">The peer address. Since pings are used the peer ID can be Number160.Zero.</param>
        /// <param name="configuration"></param>
        /// <param name="tcsDiscover"></param>
        /// <returns>The future discover. This holds also the real ID of the peer we send the discover request.</returns>
        private TcsDiscover Discover(PeerAddress peerAddress, IConnectionConfiguration configuration,
            TcsDiscover tcsDiscover)
        {
            var taskCc = _peer.ConnectionBean.Reservation.CreateAsync(1, 2);
            Utils.Utils.AddReleaseListener(taskCc, TcsDiscover.Task);
            taskCc.ContinueWith(tcc =>
            {
                if (!tcc.IsFaulted)
                {
                    Discover(tcsDiscover, peerAddress, tcc.Result, configuration);
                }
                else
                {
                    tcsDiscover.SetException(tcc.TryGetException());
                }
            });

            return tcsDiscover;
        }

        /// <summary>
        /// Needs 3 connections. Cleans up channel creator, which means they will be released.
        /// </summary>
        /// <param name="tcsDiscover"></param>
        /// <param name="peerAddress"></param>
        /// <param name="cc"></param>
        /// <param name="configuration"></param>
        private void Discover(TcsDiscover tcsDiscover, PeerAddress peerAddress, ChannelCreator cc,
            IConnectionConfiguration configuration)
        {
            _peer.PingRpc.AddPeerReachableListener(new DiscoverPeerReachableListener(tcsDiscover));

            var taskResponseTcp = _peer.PingRpc.PingTcpDiscoverAsync(peerAddress, cc, configuration, SenderAddress);
            taskResponseTcp.ContinueWith(taskResponse =>
            {
                var serverAddress = _peer.PeerBean.ServerPeerAddress;
                if (!taskResponse.IsFaulted)
                {
                    var tmp = taskResponseTcp.Result.NeighborsSet(0).Neighbors;
                    tcsDiscover.SetReporter(taskResponseTcp.Result.Sender);
                    if (tmp.Count == 1)
                    {
                        var seenAs = tmp.First();
                        Logger.Info("This peer is seen as {0} by peer {1}. This peer sees itself as {2}.", seenAs, peerAddress, _peer.PeerAddress.InetAddress);
                        if (!_peer.PeerAddress.InetAddress.Equals(seenAs.InetAddress))
                        {
                            // check if we have this interface on that we can listen to
                            var bindings = new Bindings().AddAddress(seenAs.InetAddress);
                            var status = DiscoverNetworks.DiscoverInterfaces(bindings);
                            Logger.Info("2nd interface discovery: {0}.", status);
                            if (bindings.FoundAddresses.Count > 0
                                && bindings.FoundAddresses.Contains(seenAs.InetAddress))
                            {
                                serverAddress = serverAddress.ChangeAddress(seenAs.InetAddress);
                                _peer.PeerBean.SetServerPeerAddress(serverAddress);
                                Logger.Info("This peer had the wrong interface. Changed it to {0}.", serverAddress);
                            }
                            else
                            {
                                // now we know our internal IP, where we receive packets
                                var ports =
                                    _peer.ConnectionBean.ChannelServer.ChannelServerConfiguration.PortsForwarding;
                                if (ports.IsManualPort)
                                {
                                    serverAddress = serverAddress.ChangePorts(ports.TcpPort, ports.UdpPort);
                                    serverAddress = serverAddress.ChangeAddress(seenAs.InetAddress);
                                    _peer.PeerBean.SetServerPeerAddress(serverAddress);
                                    Logger.Info("This peer had manual ports. Changed it to {0}.", serverAddress);
                                }
                                else
                                {
                                    // we need to find a relay, because there is a NAT in the way
                                    tcsDiscover.SetExternalHost(
                                        "We are most likely behind a NAT. Try to UPNP, NAT-PMP or relay " + peerAddress, taskResponseTcp.Result.Recipient.InetAddress, seenAs.InetAddress);
                                    return;
                                }
                            }
                        }
                        // else -> we announce exactly how the other peer sees us
                        var taskResponse1 = _peer.PingRpc.PingTcpProbeAsync(peerAddress, cc, configuration);
                        taskResponse1.ContinueWith(tr1 =>
                        {
                            if (tr1.IsFaulted)
                            {
                                tcsDiscover.SetException(new TaskFailedException("TcsDiscover (2): We need at least the TCP connection.", tr1));
                            }
                        });
                        
                        var taskResponse2 = _peer.PingRpc.PingUdpProbeAsync(peerAddress, cc, configuration);
                        taskResponse2.ContinueWith(tr2 =>
                        {
                            if (tr2.IsFaulted)
                            {
                                Logger.Warn("TcsDiscover (2): UDP failed connection.");
                            }
                        });

                        // from here we probe, set the timeout here
                        tcsDiscover.Timeout(serverAddress, _peer.ConnectionBean.Timer, DiscoverTimeoutSec);
                        return;
                    }
                    tcsDiscover.SetException(new TaskFailedException(String.Format("Peer {0} did not report our IP address.", peerAddress)));
                }
                else
                {
                    tcsDiscover.SetException(new TaskFailedException("TcsDiscover (1): We need at least the TCP connection.", taskResponse));
                }
            });
        }

        private class DiscoverPeerReachableListener : IPeerReachable
        {
            private readonly TcsDiscover _tcsDiscover;
            private bool _changedUdp = false;
            private bool _changedTcp = false;

            public DiscoverPeerReachableListener(TcsDiscover tcsDiscover)
            {
                _tcsDiscover = tcsDiscover;
            }

            public void PeerWellConnected(PeerAddress peerAddress, PeerAddress reporter, bool tcp)
            {
                if (tcp)
                {
                    _changedTcp = true;
                    _tcsDiscover.SetDiscoveredTcp();
                }
                else
                {
                    _changedUdp = true;
                    _tcsDiscover.SetDiscoveredUdp();
                }
                if (_changedTcp && _changedUdp)
                {
                    _tcsDiscover.Done(peerAddress, reporter);
                }
            }
        }
    }
}
