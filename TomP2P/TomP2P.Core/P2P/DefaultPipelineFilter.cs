using TomP2P.Core.Connection;
using TomP2P.Core.Connection.Windows.Netty;

namespace TomP2P.Core.P2P
{
    /// <summary>
    /// The default filter is no filter. It just returns the same pipeline.
    /// </summary>
    public class DefaultPipelineFilter : IPipelineFilter
    {
        public Pipeline Filter(Pipeline pipeline, bool isTcp, bool isClient)
        {
            return pipeline;
        }
    }
}
