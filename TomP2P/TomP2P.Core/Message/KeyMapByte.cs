using System;
using System.Collections.Generic;
using TomP2P.Core.Peers;

namespace TomP2P.Core.Message
{
    public class KeyMapByte : IEquatable<KeyMapByte>
    {
        public IDictionary<Number640, sbyte> KeysMap { get; private set; }

        public KeyMapByte(IDictionary<Number640, sbyte> keysMap)
        {
            KeysMap = keysMap;
        }

        public void Put(Number640 key, sbyte value)
        {
            KeysMap.Add(key, value);
        }

        public int Size
        {
            get { return KeysMap.Count; }
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
            return Equals(obj as KeyMapByte);
        }

        public bool Equals(KeyMapByte other)
        {
            bool t1 = Utils.Utils.IsSameSets(KeysMap.Keys, other.KeysMap.Keys);
            bool t2 = Utils.Utils.IsSameSets(KeysMap.Values, other.KeysMap.Values);

            return t1 && t2;
        }

        public override int GetHashCode()
        {
            return KeysMap.GetHashCode();
        }
    }
}
