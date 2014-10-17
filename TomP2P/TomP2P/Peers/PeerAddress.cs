using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Peers
{
    public class PeerAddress : IComparable<PeerAddress>
    {
        public Number160 PeerId { get; private set; }
        public int Size { get; private set; }


        public int CompareTo(PeerAddress other)
        {
            throw new NotImplementedException();
        }
    }

}
