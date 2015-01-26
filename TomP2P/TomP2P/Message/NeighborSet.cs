using System;
using System.Collections.Generic;
using System.Linq;
using TomP2P.Peers;

namespace TomP2P.Message
{
    public class NeighborSet : IEquatable<NeighborSet>
    {
        public int NeighborsLimit { get; private set; }
        public ICollection<PeerAddress> Neighbors { get; private set; }

        public NeighborSet(int neighborLimit, ICollection<PeerAddress> neighbors)
        {
            NeighborsLimit = neighborLimit;
            Neighbors = neighbors;

            // remove neighbors that are over the limit
            long serializedSize = 1;
            
            // no need to cut if we don't provide a limit
            if (NeighborsLimit < 0)
            {
                return;
            }

            foreach (var neighbor in neighbors.ToList()) // iterate over list-copy
            {
                serializedSize += neighbor.Size;
                if (serializedSize > neighborLimit)
                {
                    neighbors.Remove(neighbor); // remove from original list
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
            return Equals(obj as NeighborSet);
        }

        public bool Equals(NeighborSet other)
        {
            bool t1 = NeighborsLimit == other.NeighborsLimit;
            bool t2 = Utils.Utils.IsSameSets(Neighbors, other.Neighbors);

            return t1 && t2;
        }

        public override int GetHashCode()
        {
            // TODO check correctness
    	    int hash = 5;
            hash = 89 * hash + (Neighbors != null ? Neighbors.GetHashCode() : 0);
            hash = 89 * hash + (NeighborsLimit ^ (NeighborsLimit >> 32));
            return hash;
        }
    }
}
