using System;
using System.Collections.Generic;
using TomP2P.Peers;

namespace TomP2P.Message
{
    public class KeyCollection : IEquatable<KeyCollection>
    {
        public ICollection<Number640> Keys { get; private set; }
        public ICollection<Number160> KeysConvert { get; private set; }

        public Number160 LocationKey { get; private set; }
        public Number160 DomainKey { get; private set; }
        public Number160 VersionKey { get; private set; }

        public KeyCollection(Number160 locationKey, Number160 domainKey, Number160 versionKey,
            ICollection<Number160> keysConvert)
        {
            Keys = null;
            KeysConvert = keysConvert;
            LocationKey = locationKey;
            DomainKey = domainKey;
            VersionKey = versionKey;
        }

        public KeyCollection(ICollection<Number640> keys)
        {
            Keys = keys;
            KeysConvert = null;
            LocationKey = null;
            DomainKey = null;
            VersionKey = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number640">Add this number to the 480 set.</param>
        /// <returns>This class.</returns>
        public KeyCollection Add(Number640 number640)
        {
            Keys.Add(number640);
            return this;
        }

        private ICollection<Number640> Convert(KeyCollection k)
        {
            ICollection<Number640> kc;
            if (k.KeysConvert != null)
            {
                kc = new List<Number640>(k.KeysConvert.Count);
                foreach (var num160 in k.KeysConvert)
                {
                    //kc.Add(new Number640(k.LocationKey, k.DomainKey, k.VersionKey, num160));
                    kc.Add(new Number640(k.LocationKey, k.DomainKey, num160, k.VersionKey));
                }
            }
            else
            {
                kc = k.Keys;
            }
            return kc;
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

            return Equals(obj as KeyCollection);
        }

        public bool Equals(KeyCollection other)
        {
            ICollection<Number640> kc2 = Convert(this);
            ICollection<Number640> kc3 = Convert(other);
            return Utils.Utils.IsSameSets(kc2, kc3);
        }

        public override int GetHashCode()
        {
            ICollection<Number640> keys = Convert(this);
            return keys.GetHashCode();
        }

        /// <summary>
        /// The size of either the set with the Number480 as key, or the set with the Number160 as key.
        /// </summary>
        public int Size
        {
            get
            {
                if (Keys != null)
                {
                    return Keys.Count;
                }
                if (KeysConvert != null)
                {
                    return KeysConvert.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// True, if we habe Number160 stored and we need to add the location and domain key.
        /// </summary>
        public bool IsConvert
        {
            get { return KeysConvert != null; }
        }
    }
}
