using System.Collections.Generic;

namespace TomP2P.Peers
{
    public interface IMaintainable
    {
        PeerStatistic NextForMaintenance(ICollection<PeerAddress> notInterestedAddresses);
    }
}
