using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Peers;

namespace TomP2P.Message
{
    public class NeighborSet
    {
        private readonly int _neighborLimit;
        private readonly IEnumerable<PeerAddress> _neighbors;

        public NeighborSet(int neighborLimit, IEnumerable<PeerAddress> neighbors)
        {
            _neighborLimit = neighborLimit;
            _neighbors = neighbors;

            // remove neighbors that are over the limit
            int serializedSize = 1;
            
            // no need to cut if we don't provide a limit
            if (neighborLimit < 0) // TODO shouldn't this be <= 0?
            {
                return;
            }
            foreach (var neighbor in neighbors)
            {
                //serializedSize += neighbor.Size;
                //if (serializedSize > neighborLimit)

            }
        }
    }
}
