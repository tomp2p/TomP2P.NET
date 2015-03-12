using System.Collections.Generic;

namespace TomP2P.Core.Peers
{
    public interface IMaintainable
    {
        PeerStatistic NextForMaintenance(ICollection<PeerAddress> notInterestedAddresses);
    }
}
