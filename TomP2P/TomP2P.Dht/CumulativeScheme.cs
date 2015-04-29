using System;
using System.Collections.Generic;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty.Buffer;

namespace TomP2P.Dht
{
    public class CumulativeScheme : IEvaluationSchemeDht
    {
        public ICollection<Number640> Evaluate1(IDictionary<PeerAddress, IDictionary<Number640, Number160>> rawKeys)
        {
            var result = new HashSet<Number640>();
            if (rawKeys != null)
            {
                foreach (var dictionary in rawKeys.Values)
                {
                    result.AddRange(dictionary.Keys);
                }
            }
            return result;
        }

        public IDictionary<Number640, Data> Evaluate2(IDictionary<PeerAddress, IDictionary<Number640, Data>> rawData)
        {
            var result = new Dictionary<Number640, Data>();
            foreach (var dictionary in rawData.Values)
            {
                result.AddRange(dictionary);
            }
            return result;
        }

        public object Evaluate3(IDictionary<PeerAddress, object> rawObjects)
        {
            throw new NotSupportedException("Cannot be cumulated.");
        }

        public ByteBuf Evaluate4(IDictionary<PeerAddress, ByteBuf> rawChannels)
        {
            throw new NotSupportedException("Cannot be cumulated.");
        }

        public DigestResult Evaluate5(IDictionary<PeerAddress, DigestResult> rawDigest)
        {
            throw new NotSupportedException("Cannot be cumulated.");
        }

        public ICollection<Number640> Evaluate6(IDictionary<PeerAddress, IDictionary<Number640, byte>> rawKeys)
        {
            var result = new Dictionary<Number640, byte>();
            foreach (var dictionary in rawKeys.Values)
            {
                result.AddRange(dictionary);
            }
            return result.Keys;
        }
    }
}
