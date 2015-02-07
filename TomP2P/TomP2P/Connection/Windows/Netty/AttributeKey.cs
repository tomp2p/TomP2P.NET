using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TomP2P.Connection.Windows.Netty
{
    public sealed class AttributeKey<T>
    {
        private static readonly ConcurrentDictionary<string, bool> Names = new ConcurrentDictionary<string, bool>();

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
            // TODO implement
            throw new NotImplementedException();
        }
    }
}
