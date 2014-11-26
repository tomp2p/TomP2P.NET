using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Peers
{
    /// <summary>
    /// Keeps track of the statistics of a given peer.
    /// </summary>
    public class PeerStatistic
    {
        private readonly AtomicLong _lastSeenOnline = new AtomicLong(0);
        private readonly long _created = Convenient.CurrentTimeMillis();
        private readonly AtomicInteger _successfullyChecked = new AtomicInteger(0);
        private readonly AtomicInteger _failed = new AtomicInteger(0);

        private readonly Number160 _peerId;
        private PeerAddress _peerAddress;

        public PeerStatistic(PeerAddress peerAddress)
        {
            if (peerAddress == null)
            {
                throw new ArgumentException("PeerAddress cannot be null.");
            }
            _peerId = peerAddress.PeerId;
            _peerAddress = peerAddress;
        }

        /// <summary>
        /// Sets the time when last seen online to now.
        /// </summary>
        /// <returns>The number of successful checks.</returns>
        public int SuccessfullyChecked()
        {
            Interlocked.
        }

        public PeerAddress PeerAddress { get; private set; }
    }
}
