using System;
using System.Text;

namespace TomP2P.P2P
{
    public class RequestP2PConfiguration : IRequestConfiguration
    {
        public int MinimumResults { get; private set; }
        public int MaxFailure { get; private set; }
        public int ParallelDiff { get; private set; }

        // set to force either UDP or TCP
        public bool IsForceUdp  { get; private set; }
        public bool IsForceTcp  { get; private set; }

        public RequestP2PConfiguration(int minimumResults, int maxFailure, int parallelDiff)
            : this(minimumResults, maxFailure, parallelDiff, false, false)
        { }

        /// <summary>
        /// Sets the P2P/DHT configuration and its stop conditions.
        /// Based on the message size, either UDP or TCP is used.
        /// </summary>
        /// <param name="minimumResults">Stops direct calls if m peers have been contacted.</param>
        /// <param name="maxFailure">Stops the direct calls if f peers have failed.</param>
        /// <param name="parallelDiff">Use parallelDiff + minimumResults parallel connections for the P2P/DHT operation.</param>
        /// <param name="forceUdp">Flag to indicate that routing should be done with UDP instead of TCP.</param>
        /// <param name="forceTcp">Flag to indicate that routing should be done with TCP instead of UDP.</param>
        public RequestP2PConfiguration(int minimumResults, int maxFailure, int parallelDiff, bool forceUdp,
            bool forceTcp)
        {
            if (minimumResults < 0 || maxFailure < 0 || parallelDiff < 0)
            {
                throw new ArgumentException("Some values need to be larger or equal to zero.");
            }
            MinimumResults = minimumResults;
            MaxFailure = maxFailure;
            ParallelDiff = parallelDiff;
            IsForceUdp = forceUdp;
            IsForceTcp = forceTcp;
        }

        public RequestP2PConfiguration AdjustMinimumResult(int minimumresultsLow)
        {
            return new RequestP2PConfiguration(Math.Min(minimumresultsLow, MinimumResults), MaxFailure, ParallelDiff, IsForceUdp, IsForceTcp);
        }

        public int Parallel
        {
            get { return MinimumResults + ParallelDiff; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("minRes=").Append(MinimumResults)
                .Append("maxFail=").Append(MaxFailure)
                .Append("pDiff=").Append(ParallelDiff);
            return sb.ToString();
        }
    }
}
