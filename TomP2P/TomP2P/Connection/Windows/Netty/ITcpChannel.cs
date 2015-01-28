namespace TomP2P.Connection.Windows.Netty
{
    public interface ITcpChannel : IChannel
    {
        bool IsActive { get; }
    }
}
