using TomP2P.P2P.Builder;

namespace TomP2P.Connection
{
    public interface IPingFactory
    {
        PingBuilder Ping();
    }
}
