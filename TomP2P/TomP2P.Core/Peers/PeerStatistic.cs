using System;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Core.Peers
{
    /// <summary>
    /// Keeps track of the statistics of a given peer.
    /// </summary>
    public class PeerStatistic : IEquatable<PeerStatistic>
    {
        /// <summary>
        /// The time of this PeerStatistic creation.
        /// </summary>
        public long Created { private get; set; }
        
        private readonly VolatileLong _lastSeenOnline = new VolatileLong(0);
        private readonly VolatileInteger _successfullyChecked = new VolatileInteger(0);
        private readonly VolatileInteger _failed = new VolatileInteger(0);

        private readonly Number160 _peerId;
        /// <summary>
        /// The PeerAddress associated with this peer.
        /// </summary>
        public PeerAddress PeerAddress { private set; get; }

        public PeerStatistic(PeerAddress peerAddress)
        {
            if (peerAddress == null)
            {
                throw new ArgumentException("PeerAddress cannot be null.");
            }
            Created = Convenient.CurrentTimeMillis();
            _peerId = peerAddress.PeerId;
            PeerAddress = peerAddress;
        }

        /// <summary>
        /// Sets the time when last seen online to now.
        /// </summary>
        /// <returns>The number of successful checks.</returns>
        public int SuccessfullyChecked()
        {
            _lastSeenOnline.Set(Convenient.CurrentTimeMillis());
            _failed.Set(0);
            return _successfullyChecked.IncrementAndGet();
        }

        /// <summary>
        /// Increases the failed counter.
        /// </summary>
        /// <returns>The number of failed checks.</returns>
        public int Failed()
        {
            return _failed.IncrementAndGet();
        }

        /// <summary>
        /// Sets a new PeerAddress, but only if the previous had the same peer ID.
        /// </summary>
        /// <param name="peerAddress">The new peer address.</param>
        /// <returns>The old peer address.</returns>
        public PeerAddress SetPeerAddress(PeerAddress peerAddress)
        {
            if (!_peerId.Equals(peerAddress.PeerId))
            {
                throw new ArgumentException("Can only update PeerAddress with the same peer ID.");
            }
            var previous = PeerAddress;
            PeerAddress = peerAddress;
            return previous;
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
            return Equals(obj as PeerStatistic);
        }

        public bool Equals(PeerStatistic other)
        {
            return _peerId.Equals(other._peerId);
        }

        public override int GetHashCode()
        {
            return _peerId.GetHashCode();
        }

        /// <summary>
        /// Gets the time the peer has last been seen online.
        /// </summary>
        public long LastSeenOnline
        {
            get { return _lastSeenOnline.Get(); }
        }

        /// <summary>
        /// Gets the number of times the peer has been successfully checked.
        /// </summary>
        public int SuccessfullyCheckedCounter 
        {
            get { return _successfullyChecked.Get(); }
        }

        /// <summary>
        /// The time that this peer is online.
        /// </summary>
        public int OnlineTime
        {
            get { return (int)(_lastSeenOnline.Get() - Created); }
        }
    }
}
