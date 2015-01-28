namespace TomP2P.Connection.Windows.Netty
{
    public interface IUdpChannel : IChannel
    {
        bool IsOpen { get; }
    }
}
