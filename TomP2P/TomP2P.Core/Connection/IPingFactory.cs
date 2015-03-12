using TomP2P.Core.P2P.Builder;

namespace TomP2P.Core.Connection
{
    public interface IPingFactory
    {
        PingBuilder Ping();
    }
}
