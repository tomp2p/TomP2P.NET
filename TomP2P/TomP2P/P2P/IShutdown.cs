using System.Threading.Tasks;

namespace TomP2P.P2P
{
    public interface IShutdown
    {
        Task ShutdownAsync();
    }
}
