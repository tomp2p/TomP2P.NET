using System;
using System.Collections.Concurrent;
using System.Threading;

namespace TomP2P.Connection.Windows.Netty
{
    public class DefaultAttributeMap : IAttributeMap
    {
        private ConcurrentDictionary<AttributeKey, DefaultAttribute> _attributes;

        public IAttribute<T> Attr<T>(AttributeKey<T> key)
        {
            if (key == null)
            {
                throw new NullReferenceException("key");
            }

            var attributes = _attributes;
            if (attributes == null)
            {
                attributes = new ConcurrentDictionary<AttributeKey, DefaultAttribute>();

                // atomically: if _attributes == null -> replace it with attributes
                Interlocked.CompareExchange(ref _attributes, attributes, null);
            }

            var attr = attributes.GetOrAdd(key, new DefaultAttribute(key));
            return (IAttribute<T>) attr;
        }

        private sealed class DefaultAttribute<T> : DefaultAttribute, IAttribute<T>
        {
            private readonly AttributeKey<T> _key;
            private T _value;
            private readonly object _lock = new object();

            internal DefaultAttribute(AttributeKey<T> key)
                : base(key)
            {
                _key = key;
            }

            public AttributeKey<T> Key
            {
                get { return _key; }
            }

            public T Get()
            {
                lock (_lock)
                {
                    return _value;
                }
            }

            public void Set(T value)
            {
                lock (_lock)
                {
                    _value = value;
                }
            }
        }

        private class DefaultAttribute
        {
            private readonly AttributeKey _key;

            internal DefaultAttribute(AttributeKey key)
            {
                _key = key;
            }
        }
    }
}