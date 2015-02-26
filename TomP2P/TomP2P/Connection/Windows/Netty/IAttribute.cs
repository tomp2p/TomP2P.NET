namespace TomP2P.Connection.Windows.Netty
{
    public interface IAttribute<T> : IAttribute
    {
        AttributeKey<T> Key { get; }

        T Get();

        void Set(T value);
    }

    public interface IAttribute
    { }
}
