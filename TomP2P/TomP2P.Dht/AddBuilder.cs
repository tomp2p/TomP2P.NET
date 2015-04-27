using System;
using System.Collections.Generic;
using TomP2P.Core.Peers;
using TomP2P.Core.Storage;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Dht
{
    public class AddBuilder : DhtBuilder<AddBuilder>
    {
        private static readonly TcsPut TcsPutShutdown = new TcsPut(null, 0, 0);

        public ICollection<Data> DataSet { get; private set; }
        public Data Data { get; private set; }
        public bool IsList { get; private set; }
        // TODO Random needed?
 
        // static constructor
        static AddBuilder()
        {
            TcsPutShutdown.SetException(new TaskFailedException("Peer is shutting down."));
        }

        public AddBuilder(PeerDht peerDht, Number160 locationKey)
            : base(peerDht, locationKey)
        {
            SetSelf(this);
        }

        public AddBuilder SetDataSet(ICollection<Data> dataSet)
        {
            DataSet = dataSet;
            return this;
        }

        public AddBuilder SetData(Data data)
        {
            Data = data;
            return this;
        }

        public AddBuilder SetObject(object obj)
        {
            return SetData(new Data(obj));
        }

        public AddBuilder SetIsList()
        {
            return SetIsList(true);
        }

        public AddBuilder SetIsList(bool isList)
        {
            IsList = isList;
            return this;
        }

        public TcsPut Start()
        {
            if (PeerDht.Peer.IsShutdown)
            {
                return TcsPutShutdown;
            }
            PreBuild();
            if (DataSet == null)
            {
                DataSet = new List<Data>(1);
            }
            if (Data != null)
            {
                DataSet.Add(Data);
            }
            if (DataSet.Count == 0)
            {
                throw new ArgumentException("You must either set data via SetDataSet() or SetData(). Cannot add nothing.");
            }
            return PeerDht.Dht.Add(this);
        }
    }
}
