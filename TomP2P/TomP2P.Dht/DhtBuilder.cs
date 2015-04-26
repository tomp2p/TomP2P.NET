using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Core.Connection;
using TomP2P.Core.P2P.Builder;

namespace TomP2P.Dht
{
    public abstract class DhtBuilder<T> : DefaultConnectionConfiguration, IBasicBuilder<T>, IConnectionConfiguration, ISignatureBuilder<T> where T : DhtBuilder<T>
    {
        protected readonly PeerDht _peer;
    }
}
