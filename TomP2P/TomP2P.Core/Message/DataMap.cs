using System;
using System.Collections.Generic;
using TomP2P.Core.Peers;
using TomP2P.Core.Storage;

namespace TomP2P.Core.Message
{
    public class DataMap : IEquatable<DataMap>
    {
        public IDictionary<Number640, Data> BackingDataMap { get; private set; }
        public IDictionary<Number160, Data> DataMapConvert { get; private set; }

        public Number160 LocationKey { get; private set; }
        public Number160 DomainKey { get; private set; }
        public Number160 VersionKey { get; private set; }

        public bool IsConvertMeta { get; private set; }

        public DataMap(IDictionary<Number640, Data> dataMap)
            : this(dataMap, false)
        { }

        public DataMap(IDictionary<Number640, Data> dataMap, bool isConvertMeta)
        {
            BackingDataMap = dataMap;
            DataMapConvert = null;

            LocationKey = null;
            DomainKey = null;
            VersionKey = null;

            IsConvertMeta = isConvertMeta;
        }

        public DataMap(Number160 locationKey, Number160 domainKey, Number160 versionKey,
            Dictionary<Number160, Data> dataMapConvert)
            : this(locationKey, domainKey, versionKey, dataMapConvert, false)
        { }

        public DataMap(Number160 loactionKey, Number160 domainKey, Number160 versionKey,
            Dictionary<Number160, Data> dataMapConvert, bool isConvertMeta)
        {
            BackingDataMap = null;
            DataMapConvert = dataMapConvert;

            LocationKey = loactionKey;
            DomainKey = domainKey;
            VersionKey = versionKey;

            IsConvertMeta = isConvertMeta;
        }

        public IDictionary<Number640, Data> ConvertToMap640()
        {
            return Convert(this);
        }

        public Dictionary<Number640, Number160> ConvertToHash()
        {
            var result = new Dictionary<Number640, Number160>();

            if (BackingDataMap != null)
            {
                foreach (var data in BackingDataMap)
                {
                    result.Add(data.Key, data.Value.Hash);
                }
            }
            else if (DataMapConvert != null)
            {
                foreach (var data in DataMapConvert)
                {
                    result.Add(new Number640(LocationKey, DomainKey, data.Key, VersionKey), data.Value.Hash);
                }
            }
            return result;
        }

        private static IDictionary<Number640, Data> Convert(DataMap map)
        {
            IDictionary<Number640, Data> dm;
            if (map.DataMapConvert != null)
            {
                dm = new Dictionary<Number640, Data>(map.DataMapConvert.Count);

                foreach (var data in map.DataMapConvert)
                {
                    dm.Add(new Number640(map.LocationKey, map.DomainKey, data.Key, map.VersionKey), data.Value);
                }
            }
            else
            {
                dm = map.BackingDataMap;
            }
            return dm;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as DataMap);
        }

        public bool Equals(DataMap other)
        {
            IDictionary<Number640, Data> dm2 = Convert(this);
            IDictionary<Number640, Data> dm3 = Convert(other);

            bool t1 = Utils.Utils.IsSameSets(dm2.Keys, dm3.Keys);
            bool t2 = Utils.Utils.IsSameSets(dm2.Values, dm3.Values);
            return t1 && t2;
        }

        public override int GetHashCode()
        {
            IDictionary<Number640, Data> dataMap = Convert(this);
            return dataMap.GetHashCode();
        }

        /// <summary>
        /// The size of either the datamap with the Number480 as key, or the datamap with the Number160 as key.
        /// </summary>
        public int Size
        {
            get
            {
                if (BackingDataMap != null)
                {
                    return BackingDataMap.Count;
                }
                if (DataMapConvert != null)
                {
                    return DataMapConvert.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// True, if we have Number160 stored and we need to add the location and domain key.
        /// </summary>
        public bool IsConvert
        {
            get { return DataMapConvert != null; }
        }
    }
}
