using System.Collections.Generic;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;
using TomP2P.Extensions.Netty.Buffer;

namespace TomP2P.Dht
{
    public interface IEvaluationSchemeDht
    {
        ICollection<Number640> Evaluate1(IDictionary<PeerAddress, IDictionary<Number640, Number160>> rawKeys);

        IDictionary<Number640, Data> Evaluate2(IDictionary<PeerAddress, IDictionary<Number640, Data>> rawData);

        object Evaluate3(IDictionary<PeerAddress, object> rawObjects);

        ByteBuf Evaluate4(IDictionary<PeerAddress, ByteBuf> rawChannels);

        DigestResult Evaluate5(IDictionary<PeerAddress, DigestResult> rawDigest);

        ICollection<Number640> Evaluate6(IDictionary<PeerAddress, IDictionary<Number640, byte>> rawKeys);
    }
}
