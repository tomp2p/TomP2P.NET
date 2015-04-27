using System;
using System.Collections.Generic;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;
using TomP2P.Extensions.Netty.Buffer;

namespace TomP2P.Dht
{
    public class VotingSchemeDht : IEvaluationSchemeDht
    {
        private static readonly SortedDictionary<Number640, ICollection<Number160>> EmptyMap = new SortedDictionary<Number640, ICollection<Number160>>();

        public ICollection<Number640> Evaluate1(IDictionary<PeerAddress, IDictionary<Number640, Number160>> rawKeys)
        {
            var counter = new Dictionary<Number640, int?>();
            var result = new HashSet<Number640>();

            var size = rawKeys == null ? 0 : rawKeys.Count;
            var majority = (size + 1)/2;

            if (rawKeys != null)
            {
                foreach (var peerAddress in rawKeys.Keys)
                {
                    var keys = rawKeys[peerAddress].Keys;
                    foreach (var num640 in keys)
                    {
                        var c = 1;
                        var count = counter[num640];
                        if (count != null)
                        {
                            c = count.Value + 1;
                        }
                        counter.Add(num640, c);
                        if (c >= majority)
                        {
                            result.Add(num640);
                        }
                    }
                }
            }
            return result;
        }

        public IDictionary<Number640, Data> Evaluate2(IDictionary<PeerAddress, IDictionary<Number640, Data>> rawData)
        {
            if (rawData == null)
            {
                throw new ArgumentException("rawData");
            }
            var counter = new Dictionary<Number160, int?>();
            var result = new Dictionary<Number640, Data>();

            var size = rawData.Count;
            var majority = (size + 1)/2;

            foreach (var peerAddress in rawData.Keys)
            {
                var dictionary = rawData[peerAddress];
                foreach (var num640 in dictionary.Keys)
                {
                    var data = dictionary[num640];
                    var hash = data.Hash
                        .Xor(num640.ContentKey)
                        .Xor(num640.DomainKey)
                        .Xor(num640.LocationKey);
                    var c = 1;
                    var count = counter[hash];
                    if (count != null)
                    {
                        c = count.Value + 1;
                    }
                    counter.Add(hash, c);
                    if (c >= majority)
                    {
                        result.Add(num640, data);
                    }
                }
            }
            return result;
        }

        public object Evaluate3(IDictionary<PeerAddress, object> rawObjects)
        {
            return Evaluate0(rawObjects);
        }

        public ByteBuf Evaluate4(IDictionary<PeerAddress, ByteBuf> rawChannels)
        {
            return Evaluate0(rawChannels);
        }

        public DigestResult Evaluate5(IDictionary<PeerAddress, DigestResult> rawDigest)
        {
            var digestRes = Evaluate0(rawDigest);
            // If it is null, we know that we did not get any results.
            // In order to return null, we return an empty digest result.
            return digestRes ?? new DigestResult(EmptyMap);
        }

        public ICollection<Number640> Evaluate6(IDictionary<PeerAddress, IDictionary<Number640, byte>> rawKeys)
        {
            var counter = new Dictionary<Number640, int?>();
            var result = new HashSet<Number640>();

            var size = rawKeys == null ? 0 : rawKeys.Count;
            var majority = (size + 1)/2;

            if (rawKeys != null)
            {
                foreach (var peerAddress in rawKeys.Keys)
                {
                    var keys = rawKeys[peerAddress].Keys;
                    foreach (var num640 in keys)
                    {
                        var c = 1;
                        var count = counter[num640];
                        if (count != null)
                        {
                            c = count.Value + 1;
                        }
                        counter.Add(num640, c);
                        if (c >= majority)
                        {
                            result.Add(num640);
                        }
                    }
                }
            }
            return result;
        }

        private static T Evaluate0<T>(IDictionary<PeerAddress, T> raw) where T : class 
        {
            if (raw == null)
            {
                throw new ArgumentException("raw");
            }
            var counter = new Dictionary<T, int?>();
            T best = null;
            var count = 0;
            foreach (var peerAddress in raw.Keys)
            {
                var t = raw[peerAddress];
                if (t != null)
                {
                    var c = counter[t];
                    if (c == null)
                    {
                        c = 0;
                    }
                    c++;
                    counter.Add(t, c);
                    if (c > count)
                    {
                        best = t;
                        count = c.Value;
                    }
                }
            }
            return best;
        }
    }
}
