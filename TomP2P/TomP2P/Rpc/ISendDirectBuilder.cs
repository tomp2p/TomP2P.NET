using TomP2P.Connection;
using TomP2P.Extensions.Workaround;
using TomP2P.Message;
using TomP2P.P2P;

namespace TomP2P.Rpc
{
    public interface ISendDirectBuilder : IConnectionConfiguration
    {
        bool IsRaw { get; }

        // TODO ProgressListener needed?

        bool IsSign { get; }

        bool IsStreaming { get; }

        Buffer Buffer { get; }

        object Object { get; }

        KeyPair KeyPair { get; }
    }
}
