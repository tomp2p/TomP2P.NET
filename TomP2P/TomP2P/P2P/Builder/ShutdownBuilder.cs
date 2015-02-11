using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection;

namespace TomP2P.P2P.Builder
{
    /// <summary>
    /// Sets the configuration options for the shutdown command. The shutdown does first a routing, searches
    /// for its close peers and then sends a quit message so that the other peers know that this peer is offline.
    /// </summary>
    public class ShutdownBuilder : DefaultConnectionConfiguration, ISignatureBuilder<ShutdownBuilder>
    {
    }
}
