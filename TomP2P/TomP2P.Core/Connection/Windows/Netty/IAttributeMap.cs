namespace TomP2P.Core.Connection.Windows.Netty
{
    public interface IAttributeMap
    {
        IAttribute<T> Attr<T>(AttributeKey<T> key);
    }
}
