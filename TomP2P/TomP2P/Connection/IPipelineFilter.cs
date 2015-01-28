using TomP2P.Extensions.Netty;

namespace TomP2P.Connection
{
    /// <summary>
    /// The user may modify the filter by adding, removing or changing the handlers.
    /// </summary>
    public interface IPipelineFilter
    {
        /// <summary>
        /// Filters the handlers. If no filtering should happen, return the same pipeline.
        /// </summary>
        /// <param name="pipeline">The handlers created by TomP2P.</param>
        /// <param name="isTcp">True, if the connection is TCP. False, if UDP.</param>
        /// <param name="isClient">True, if this is the client side. False, if server side.</param>
        /// <returns>The same, new or changed pipeline.</returns>
        Pipeline Filter(Pipeline pipeline, bool isTcp, bool isClient);
    }
}
