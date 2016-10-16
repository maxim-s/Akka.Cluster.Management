using System;

namespace Akka.Cluster.Management
{
    public class ClusterDiscoverySettings
    {
        /// <summary>
        /// Path, relative to seeds, where seed nodes information is stored
        /// </summary>
        public string SeedsPath { get; set; }

        public TimeSpan LeaderEntryTTL { get; set; }

        public TimeSpan RetryDelay { get; set; }

        public string LeaderPath { get; }

        public TimeSpan SeedsJoinTimeout { get; set; }
        public TimeSpan SeedsFetchTimeout { get; set; }
        public string BasePath { get; set; }
    }
}