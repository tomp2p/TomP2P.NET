using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    /// <summary>
    /// A bean that holds non-sharable (unique for each peer) configuration settings for the peer.
    /// The sharable configurations are stored in a <see cref="ConnectionBean"/>.
    /// </summary>
    public class PeerBean
    {
        public PeerAddress ServerPeerAddress()
        {
            throw new NotImplementedException();
        }
    }
}
