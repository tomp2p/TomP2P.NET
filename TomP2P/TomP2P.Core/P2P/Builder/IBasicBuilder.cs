using System.Collections.Generic;
using TomP2P.Core.Connection;
using TomP2P.Core.Peers;

namespace TomP2P.Core.P2P.Builder
{
    /// <summary>
    /// The basic build methods for the builder classes.
    /// </summary>
    public interface IBasicBuilder<out T> : IConnectionConfiguration, IBuilder
    {
        Number160 LocationKey { get; }

        Number160 DomainKey { get; }

        T SetDomainKey(Number160 domainKey);

        RoutingConfiguration RoutingConfiguration { get; }

        T SetRoutingConfiguration(RoutingConfiguration routingConfiguration);

        RequestP2PConfiguration RequestP2PConfiguration { get; }

        T SetRequestP2PConfiguration(RequestP2PConfiguration requestP2PConfiguration);

        RoutingBuilder CreateBuilder(RequestP2PConfiguration requestP2PConfiguration,
            RoutingConfiguration routingConfiguration);

        /// <summary>
        /// A set of filters or null if not filters are set.
        /// </summary>
        ICollection<IPeerMapFilter> PeerMapFilters { get; }

        /// <summary>
        /// A set of filters or null if not filters are set.
        /// </summary>
        ICollection<IPostRoutingFilter> PostRoutingFilters { get; } 
    }
}
