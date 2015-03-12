using System;

namespace TomP2P.Core.P2P
{
    public class RoutingConfiguration
    {
        /// <summary>
        /// Number of direct hits (d):
        /// This is used for fetching data. If d peers have been contacted that have the data stored, routing stops.
        /// </summary>
        public int MaxDirectHits { get; private set; }
        /// <summary>
        /// Number of no new information (n):
        /// This is mainly used for storing data. It searches the closest peers and if n peers do not report any closer nodes, the routing stops.
        /// </summary>
        public int MaxNoNewInfoDiff { get; private set; }
        /// <summary>
        /// Number of failures (f):
        /// The routing stops if f peers fail to respond.
        /// </summary>
        public int MaxFailures { get; private set; }
        /// <summary>
        /// Number of success (s):
        /// The routing stops if s peers respond.
        /// </summary>
        public int MaxSuccess { get; private set; }
        /// <summary>
        /// Number of parallel requests (p):
        /// This tells the routing how many peers to contact in parallel.
        /// </summary>
        public int Parallel { get; private set; }
        /// <summary>
        /// Flag to indicate that routing should be done with TCP instead of UDP.
        /// </summary>
        public bool ForceTcp { get; private set; }

        public RoutingConfiguration(int maxNoNewInfoDiff, int maxFailures, int parallel)
            : this(Int32.MaxValue, maxNoNewInfoDiff, maxFailures, 20, parallel)
        { }

        public RoutingConfiguration(int maxNoNewInfoDiff, int maxFailures, int maxSuccess, int parallel)
            : this(Int32.MaxValue, maxNoNewInfoDiff, maxFailures, maxSuccess, parallel)
        { }

        public RoutingConfiguration(int directHits, int maxNoNewInfoDiff, int maxFailures, int maxSuccess, int parallel)
            : this(directHits, maxNoNewInfoDiff, maxFailures, maxSuccess, parallel, false)
        { }

        /// <summary>
        /// Sets the routing configuration and its stop conditions.
        /// </summary>
        /// <param name="maxDirectHits">Number of direct hits (d):
        /// This is used for fetching data. If d peers have been contacted that have the data stored, routing stops.</param>
        /// <param name="maxNoNewInfoDiff">Number of no new information (n):
        /// This is mainly used for storing data. It searches the closest peers and if n peers do not report any closer nodes, the routing stops.</param>
        /// <param name="maxFailures">Number of failures (f):
        /// The routing stops if f peers fail to respond.</param>
        /// <param name="maxSuccess">Number of success (s):
        /// The routing stops if s peers respond.</param>
        /// <param name="parallel">Number of parallel requests (p):
        /// This tells the routing how many peers to contact in parallel.</param>
        /// <param name="forceTcp">Flag to indicate that routing should be done with TCP instead of UDP.</param>
        public RoutingConfiguration(int maxDirectHits, int maxNoNewInfoDiff, int maxFailures,
            int maxSuccess, int parallel, bool forceTcp)
        {
            if (maxDirectHits < 0 || maxNoNewInfoDiff < 0 || maxFailures < 0 || parallel < 0)
            {
                throw new ArgumentException("Some arguments need to be larger than or equals to zero.");
            }
            MaxDirectHits = maxDirectHits;
            MaxNoNewInfoDiff = maxNoNewInfoDiff;
            MaxFailures = maxFailures;
            MaxSuccess = maxSuccess;
            Parallel = parallel;
            ForceTcp = forceTcp;
        }

        public int MaxNoNewInfo(int minimumResults)
        {
            return MaxNoNewInfoDiff + minimumResults;
        }
    }
}
