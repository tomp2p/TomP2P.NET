using System.Threading.Tasks;

namespace TomP2P.Core.P2P
{
    public interface IShutdown
    {
        Task ShutdownAsync();
    }
}
