using System;
using System.Collections.Generic;
using TomP2P.Peers;

namespace TomP2P.Message
{
    public class KeyMap640Keys : IEquatable<KeyMap640Keys>
    {
        public SortedDictionary<Number640, ICollection<Number160>> KeysMap { get; private set; }

        public KeyMap640Keys(SortedDictionary<Number640, ICollection<Number160>> keysMap)
        {
            KeysMap = keysMap;
        }

        public void Put(Number640 key, HashSet<Number160> value)
        {
            KeysMap.Add(key, value);
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
            return Equals(obj as KeyMap640Keys);
        }

        public bool Equals(KeyMap640Keys other)
        {
            bool t1 = Utils.Utils.IsSameSets(KeysMap.Keys, other.KeysMap.Keys);
            bool t2 = Utils.Utils.IsSameCollectionSets(KeysMap.Values, other.KeysMap.Values); // TODO this won't work

            return t1 && t2;
        }

        public override int GetHashCode()
        {
            return KeysMap.GetHashCode();
        }

        public int Size
        {
            get { return KeysMap.Count; }
        }
    }
}
