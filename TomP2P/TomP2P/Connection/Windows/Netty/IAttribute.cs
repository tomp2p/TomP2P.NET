using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
{
    public interface IAttribute<T>
    {
        AttributeKey<T> Key { get; }

        T Get();

        void Set(T value);
    }
}
