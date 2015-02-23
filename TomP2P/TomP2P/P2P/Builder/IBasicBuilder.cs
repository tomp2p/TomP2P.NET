using System.Collections.Generic;
using TomP2P.Connection;
using TomP2P.Peers;

namespace TomP2P.P2P.Builder
{
    /// <summary>
    /// The basic build methods for the builder classes.
    /// </summary>
    public interface IBasicBuilder<T> : IConnectionConfiguration, IBuilder
    {
        Number160 LocationKey { get; }

        Number160 DomainKey { get; }

        T GetDomainKey(Number160 domainKey);

        RoutingConfiguration RoutingConfiguration { get; }

        T GetRoutingConfiguration(RoutingConfiguration routingConfiguration);

        RequestP2PConfiguration RequestP2PConfiguration { get; }

        T GetRequestP2PConfiguration(RequestP2PConfiguration requestP2PConfiguration);

        RoutingBuilder CreateBuilder(RequestP2PConfiguration requestP2PConfiguration,
            RoutingConfiguration routingConfiguration);

        /// <summary>
        /// A set of filters or null if not filters are set.
        /// </summary>
        ICollection<IPeerFilter> PeerFilters { get; } 
    }
}
