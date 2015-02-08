using System;
using System.Collections.Concurrent;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Connection.Windows.Netty
{
    public sealed class AttributeKey<T> : AttributeKey, IComparable<AttributeKey<T>>
    {
// ReSharper disable once StaticFieldInGenericType
        private static readonly ConcurrentDictionary<string, bool> Names = new ConcurrentDictionary<string, bool>();

// ReSharper disable once StaticFieldInGenericType
        private static VolatileInteger _nextId = new VolatileInteger(0);



        public static AttributeKey<T> ValueOf(string name)
        {
            return new AttributeKey<T>(name);
        }

        public AttributeKey(string name)
        {
            if (Names == null)
            {
                throw new NullReferenceException("Names");
            }
            if (name == null)
            {
                throw new NullReferenceException("name");
            }

            Names.AddOrUpdate(name, true, delegate {
                throw new ArgumentException(String.Format("'{0}' is already in use.", name));
            });

            Id = _nextId.IncrementAndGet();
            Name = name;
        }

        public int CompareTo(AttributeKey<T> other)
        {
            if (this == other)
            {
                return 0;
            }
            int returnCode = String.Compare(Name, other.Name, StringComparison.Ordinal);
            return returnCode != 0 ? returnCode : Id.CompareTo(other.Id);
        }
    }

    public class AttributeKey
    {
        public int Id { get; protected set; }
        public string Name { get; protected set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
