using System;

namespace Akka.Cluster.Management
{
    public class ClusterDiscoverySettings
    {
        public string SeedsPath { get; set; }

        public TimeSpan LeaderEntryTTL { get; set; }

        public TimeSpan RetryDelay { get; set; }

        public string LeaderPath { get; }

        public TimeSpan SeedsJoinTimeout { get; set; }
        public TimeSpan SeedsFetchTimeout { get; set; }
    }
}