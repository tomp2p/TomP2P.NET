using TomP2P.Core.Connection;
using TomP2P.Core.Message;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Core.Rpc
{
    public interface ISendDirectBuilder : IConnectionConfiguration
    {
        bool IsRaw { get; }

        bool IsSign { get; }

        bool IsStreaming { get; }

        Buffer Buffer { get; }

        object Object { get; }

        KeyPair KeyPair { get; }
    }
}
