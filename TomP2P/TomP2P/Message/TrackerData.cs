using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TomP2P.Peers;
using TomP2P.Storage;

namespace TomP2P.Message
{
    public class TrackerData : IEquatable<TrackerData>
    {
        private static readonly Data EmptyData = new Data(0, 0);

        private readonly IDictionary<PeerStatistic, Data> _peerAddresses;
        public bool CouldProvideMoreData { get; private set; }

        public TrackerData(IDictionary<PeerStatistic, Data> peerAddresses)
            : this(peerAddresses, false)
        { }

        public TrackerData(IDictionary<PeerStatistic, Data> peerAddresses, bool couldProvideMoreData)
        {
            _peerAddresses = peerAddresses;
            CouldProvideMoreData = couldProvideMoreData;
        }

        public void Put(PeerStatistic remotePeer, Data attachment)
        {
            // TODO possible NullPointerException
            _peerAddresses.Add(remotePeer, attachment ?? EmptyData);
        }

        public KeyValuePair<PeerStatistic, Data>? Remove(Number160 remotePeerId)
        {
            // TODO check if LINQ is calculated multiple times (2x)
            // TODO this might throw an exception (removing from current iteration) (2x)
            foreach (var peerAddress in _peerAddresses.Where(peerAddress => peerAddress.Key.PeerAddress.PeerId.Equals(remotePeerId)))
            {
                _peerAddresses.Remove(peerAddress);
                return peerAddress;
            }
            return null;
        }

        public bool ContainsKey(Number160 key)
        {
            return _peerAddresses.Any(peerAddress => peerAddress.Key.PeerAddress.PeerId.Equals(key));
        }

        public KeyValuePair<PeerStatistic, Data>? Get(Number160 key)
        {
            foreach (var peerAddress in _peerAddresses.Where(peerAddress => peerAddress.Key.PeerAddress.PeerId.Equals(key)))
            {
                return peerAddress;
            }
            return null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("tdata:");
            if (_peerAddresses != null)
            {
                sb.Append("p:").Append(_peerAddresses);
            }
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
            return PeerAddresses.GetHashCode();
        }

        public IDictionary<PeerStatistic, Data> PeerAddresses
        {
            get { return _peerAddresses ?? new Dictionary<PeerStatistic, Data>(); }
        }

        public int Size
        {
            get { return _peerAddresses.Count; }
        }
    }
}
