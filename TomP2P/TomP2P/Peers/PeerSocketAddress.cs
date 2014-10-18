using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Peers
{
    // TODO implement Serializable?
    public class PeerSocketAddress
    {
        public int Offset { get; private set; }

        public static PeerSocketAddress Create(byte[] me, bool isIPv4, int offsetOriginal)
        {
            throw new NotImplementedException();
        }
    }
}
