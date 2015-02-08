
namespace TomP2P.Connection.Windows.Netty
{
    public interface IAttributeMap
    {
        IAttribute<T> Attr<T>(AttributeKey<T> key);
    }
}
