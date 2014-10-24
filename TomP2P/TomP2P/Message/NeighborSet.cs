using System.Collections.Generic;
using System.Linq;
using TomP2P.Peers;

namespace TomP2P.Message
{
    public class NeighborSet
    {
        public int NeighborsLimit { get; private set; }
        public ICollection<PeerAddress> Neighbors { get; private set; }

        public NeighborSet(int neighborLimit, IEnumerable<PeerAddress> neighbors)
        {
            NeighborsLimit = neighborLimit;
            IList<PeerAddress> peerAddresses = neighbors as IList<PeerAddress> ?? neighbors.ToList();
            Neighbors = peerAddresses;

            // remove neighbors that are over the limit
            long serializedSize = 1;
            
            // no need to cut if we don't provide a limit
            if (NeighborsLimit < 0) // TODO shouldn't this be <= 0?
            {
                return;
            }
            foreach (var neighbor in peerAddresses)
            {
                serializedSize += neighbor.Size;
                if (serializedSize > NeighborsLimit)
                {
                    peerAddresses.Remove(neighbor); // TODO correct comparator?
                }
            }
        }

        public void Add(PeerAddress neighbor)
        {
            Neighbors.Add(neighbor);
        }

        public void AddResult(IEnumerable<PeerAddress> neighbors)
        {
            foreach (var neighbor in neighbors)
            {
                Neighbors.Add(neighbor);
            }
        }

        public int Size
        {
            get { return Neighbors.Count; }
        }
    }
}
