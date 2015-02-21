using System;
using System.Net;
using System.Threading;
using TomP2P.Extensions;
using TomP2P.Peers;

namespace TomP2P.Futures
{
    /// <summary>
    /// The future discover keeps track of network discovery such as discovery if it is behind a NAT,
    /// the status if UPNP or NAT-PMP could be established, if there is port forwarding.
    /// </summary>
    public class TcsDiscover : BaseTcsImpl
    {
        // result
        private PeerAddress _ourPeerAddress;
        private PeerAddress _reporter;
        private bool _discoveredTcp = false;
        private bool _discoveredUdp = false;
        private bool _isNat = false;
        private IPAddress _internalAddress;
        private IPAddress _externalAddress;

        /// <summary>
        /// Creates a new future object and creates a timer that fires failed after a timeout.
        /// </summary>
        /// <param name="serverPeerAddress"></param>
        /// <param name="timer">The timer to use.</param>
        /// <param name="delaySec">The delay in seconds.</param>
        public void Timeout(PeerAddress serverPeerAddress, Timer timer, int delaySec)
        {
            // .NET-specific
            var cts = new CancellationTokenSource();

            long start = Convenient.CurrentTimeMillis();
            var delay = System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(delaySec).Milliseconds, cts.Token);
            delay.ContinueWith(taskDelay =>
            {
                // "DiscoverTimeoutTask"
                // In case of no peer contacting us, we fire a failed
                var ms = Convenient.CurrentTimeMillis() - start;
                Failed(serverPeerAddress, String.Format("Timeout in Discover: {0} ms. However, I think my peer address is {1}.", ms, serverPeerAddress));

            }, cts.Token);

            // cancel timeout if we are done
            this.Task.ContinueWith(taskDiscover => cts.Cancel()); // TODO works?
        }

        private void Failed(PeerAddress serverPeerAddress, string failed)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                // TODO reason and type needed?
                _ourPeerAddress = serverPeerAddress;
            }
            NotifyListeners();
        }

        /// <summary>
        /// Gets called if the discovery was a success and another peer could ping us with TCP and UDP.
        /// </summary>
        /// <param name="ourPeerAddress">The peer address of our server.</param>
        /// <param name="reporter">The peer address of the peer that reported our address.</param>
        public void Done(PeerAddress ourPeerAddress, PeerAddress reporter)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                // TODO reason and type needed?
                if (_reporter != null)
                {
                    /*if (!_reporter.Equals(reporter))
                    {
                        // TODO reason and type needed?
                    }*/
                }
                _ourPeerAddress = ourPeerAddress;
                _reporter = reporter;
            }
            NotifyListeners();
        }

        /// <summary>
        /// The peer address where we are reachable.
        /// </summary>
        public PeerAddress PeerAddress
        {
            get
            {
                lock (Lock)
                {
                    return _ourPeerAddress;
                }
            }
        }

        /// <summary>
        /// The reporter that told us what peer address we have.
        /// </summary>
        public PeerAddress Reporter
        {
            get
            {
                lock (Lock)
                {
                    return _reporter;
                }
            }
        }

        public TcsDiscover SetReporter(PeerAddress reporter)
        {
            lock (Lock)
            {
                if (_reporter != null)
                {
                    if (!_reporter.Equals(reporter))
                    {
                        throw new ArgumentException("Cannot change reporter once it is set.");
                    }
                }
                _reporter = reporter;
            }
            return this;
        }

        /// <summary>
        /// Intermediate result if TCP has been discovered.
        /// Set discoveredTcp to true if other peer could reach us with a TCP ping.
        /// </summary>
        public void SetDiscoveredTcp()
        {
            lock (Lock)
            {
                _discoveredTcp = true;
            }
        }

        /// <summary>
        /// Intermediate result if UDP has been discovered.
        /// Set discoveredTcp to true if other peer could reach us with a UDP ping.
        /// </summary>
        public void SetDiscoveredUdp()
        {
            lock (Lock)
            {
                _discoveredUdp = true;
            }
        }

        /// <summary>
        /// Checks if this peer can be reached via TCP.
        /// </summary>
        public bool IsDiscoveredTcp
        {
            get
            {
                lock (Lock)
                {
                    return _discoveredTcp;
                }
            }
        }

        /// <summary>
        /// Checks if this peer can be reached via UDP.
        /// </summary>
        public bool IsDiscoveredUdp
        {
            get
            {
                lock (Lock)
                {
                    return _discoveredUdp;
                }
            }
        }

        public TcsDiscover SetExternalHost(string failed, IPAddress internalAddress, IPAddress externalAddress)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return this;
                }
                // TODO reason and type needed?
                _internalAddress = internalAddress;
                _externalAddress = externalAddress;
                _isNat = true;
            }
            NotifyListeners();
            return this;
        }

        public IPAddress InternalAddress
        {
            get
            {
                lock (Lock)
                {
                    return _internalAddress;
                }
            }
        }

        public IPAddress ExternalAddress
        {
            get
            {
                lock (Lock)
                {
                    return _externalAddress;
                }
            }
        }

        public bool IsNat
        {
            get
            {
                lock (Lock)
                {
                    return _isNat;
                }
            }
        }
    }
}
