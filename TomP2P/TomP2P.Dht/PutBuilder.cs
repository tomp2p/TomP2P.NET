using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Core.Peers;
using TomP2P.Core.Storage;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Dht
{
    public class PutBuilder : DhtBuilder<PutBuilder>
    {
        private static readonly TcsPut TcsPutShutdown = new TcsPut(null, 0, 0);

        public KeyValuePair<Number640, Data> Data { get; private set; }
        public SortedDictionary<Number640, Data> DataMap { get; private set; }
        public SortedDictionary<Number160, Data> DataMapConvert { get; private set; }

        public bool IsPutIfAbsent { get; private set; }
        public bool IsPutMeta { get; private set; }
        public bool IsPutConfirm { get; private set; }

        private IPublicKey _changePublicKey;

        // static constructor
        static PutBuilder()
        {
            TcsPutShutdown.SetException(new TaskFailedException("Peer is shutting down."));
        }

        public PutBuilder(PeerDht peerDht, Number160 locationKey)
            : base(peerDht, locationKey)
        {
            SetSelf(this);
        }

        // TODO find proper way for SetData APIs
        public PutBuilder SetData(Data data)
        {
            return SetData(LocationKey, DomainKey ?? Number160.Zero, Number160.Zero, VersionKey ?? Number160.Zero, data);
        }

        public PutBuilder SetData(Number160 contentKey, Data data)
        {
            return SetData(LocationKey, DomainKey ?? Number160.Zero, contentKey, VersionKey ?? Number160.Zero, data);
        }

        public PutBuilder SetData(Number160 domainKey, Number160 contentKey, Data data)
        {
            return SetData(LocationKey, domainKey, contentKey, VersionKey ?? Number160.Zero, data);
        }

        public PutBuilder SetData(Data data, Number160 versionKey)
        {
            return SetData(LocationKey, DomainKey ?? Number160.Zero, Number160.Zero, versionKey, data);
        }

        public PutBuilder SetData(Number160 contentKey, Data data, Number160 versionKey)
        {
            return SetData(LocationKey, DomainKey ?? Number160.Zero, contentKey, versionKey, data);
        }

        public PutBuilder SetData(Number160 locationKey, Number160 domainKey, Number160 contentKey, Number160 versionKey, Data data)
        {
            Data = new KeyValuePair<Number640, Data>(new Number640(locationKey, domainKey, contentKey, versionKey), data);
            return this;
        }

        public override PutBuilder SetDomainKey(Number160 domainKey)
        {
            // if we set data before we set the domain key, we need to adapt the domain key of the data object
            if (Data.Key != null) // TODO check if correct
            {
                SetData(Data.Key.LocationKey, domainKey, Data.Key.ContentKey, Data.Key.VersionKey, Data.Value);
            }
            base.SetDomainKey(domainKey);
            return this;
        }

        public override PutBuilder SetVersionKey(Number160 versionKey)
        {
            // if we set data before we set the version key, we need to adapt the version key of the data object
            if (Data.Key != null) // TODO check if correct
            {
                SetData(Data.Key.LocationKey, Data.Key.DomainKey, Data.Key.ContentKey, versionKey, Data.Value);
            }
            base.SetVersionKey(versionKey);
            return this;
        }

        public PutBuilder SetObject(object obj)
        {
            return SetData(new Data(obj));
        }

        public PutBuilder SetKeyObject(Number160 contentKey, object obj)
        {
            return SetData(contentKey, new Data(obj));
        }
    }
}
