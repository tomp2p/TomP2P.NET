using System;
using TomP2P.Peers;

namespace TomP2P.P2P
{
    public class Statistics
    {
        private static readonly double Max = Math.Pow(2, Number160.Bits);

        private double _estimatedNumberOfPeers = 1;
        public double AvgGap { get; private set; }
        private readonly PeerMap _peerMap;

        public Statistics(PeerMap peerMap)
        {
            _peerMap = peerMap;
            AvgGap = Max / 2;
        }

        public double EstimatedNumberOfNodes()
        {
            int bagSize = _peerMap.BagSizeVerified;
            var map = _peerMap.PeerMapVerified;

            // assume we are full
            double gap = 0D;
            int gapCount = 0;
            for (int i = 0; i < Number160.Bits; i++)
            {
                var peers = map[i];
                int numPeers = peers.Count;

                if (numPeers > 0 && numPeers < bagSize)
                {
                    double currentGap = Math.Pow(2, i)/numPeers;
                    gap += currentGap*numPeers;
                    gapCount += numPeers;
                }
                else if (numPeers == 0)
                {
                    // we are empty
                }
                else if (numPeers == bagSize)
                {
                    // we are full
                }
            }
            AvgGap = gap/gapCount;
            _estimatedNumberOfPeers = (Max/AvgGap);
            return _estimatedNumberOfPeers;
        }
    }
}
