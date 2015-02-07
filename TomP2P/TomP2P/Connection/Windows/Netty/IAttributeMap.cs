using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
{
    public interface IAttributeMap
    {
        IAttribute<T> Attr<T>(AttributeKey<T> key);
    }
}
