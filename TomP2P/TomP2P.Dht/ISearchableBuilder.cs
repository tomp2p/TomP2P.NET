using System.Collections.Generic;
using TomP2P.Core.Peers;

namespace TomP2P.Dht
{
    public interface ISearchableBuilder
    {
        Number640 From { get; }

        Number640 To { get; }

        ICollection<Number160> ContentKeys { get; } 
    }
}
