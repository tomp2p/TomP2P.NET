using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TomP2P.Core.Peers;
using TomP2P.Core.Storage;

namespace TomP2P.Core.Message
{
    public class TrackerData : IEquatable<TrackerData>
    {
        private static readonly Data EmptyData = new Data(0, 0);

        private readonly IDictionary<PeerAddress, Data> _peerAddresses;
        public bool CouldProvideMoreData { get; private set; }

        public TrackerData(IDictionary<PeerAddress, Data> peerAddresses)
            : this(peerAddresses, false)
        { }

        public TrackerData(IDictionary<PeerAddress, Data> peerAddresses, bool couldProvideMoreData)
        {
            if (peerAddresses == null)
            {
                throw new ArgumentException("Peer addresses must be set.");
            }
            _peerAddresses = peerAddresses;
            CouldProvideMoreData = couldProvideMoreData;
        }

        public void Put(PeerAddress remotePeer, Data attachment)
        {
            _peerAddresses.Add(remotePeer, attachment ?? EmptyData);
        }

        public KeyValuePair<PeerAddress, Data>? Remove(Number160 remotePeerId)
        {
            // TODO check if LINQ is calculated multiple times (2x)
            // TODO this might throw an exception (removing from current iteration) (2x)
            foreach (var peerAddress in _peerAddresses.Where(peerAddress => peerAddress.Key.PeerId.Equals(remotePeerId)))
            {
                _peerAddresses.Remove(peerAddress);
                return peerAddress;
            }
            return null;
        }

        public bool ContainsKey(Number160 key)
        {
            return _peerAddresses.Any(peerAddress => peerAddress.Key.PeerId.Equals(key));
        }

        public KeyValuePair<PeerAddress, Data>? Get(Number160 key)
        {
            foreach (var peerAddress in _peerAddresses.Where(peerAddress => peerAddress.Key.PeerId.Equals(key)))
            {
                return peerAddress;
            }
            return null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("tdata:");
            sb.Append("p:").Append(_peerAddresses);
            return sb.ToString();
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
            return Equals(obj as TrackerData);
        }

        public bool Equals(TrackerData other)
        {
            return Utils.Utils.IsSameSets(other.PeerAddresses.Keys, PeerAddresses.Keys)
                   && Utils.Utils.IsSameSets(other.PeerAddresses.Values, PeerAddresses.Values);
        }

        public override int GetHashCode()
        {
            int hashCode = 31;
            foreach (var entry in PeerAddresses)
            {
                hashCode ^= entry.Key.GetHashCode();
                if (entry.Value != null)
                {
                    hashCode ^= entry.Value.GetHashCode();
                }
            }
            return hashCode;
        }

        public IDictionary<PeerAddress, Data> PeerAddresses
        {
            get { return _peerAddresses; }
        }

        public int Size
        {
            get { return _peerAddresses.Count; }
        }
    }
}
