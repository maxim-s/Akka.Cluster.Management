namespace Akka.Cluster.Management
{
    public class ClusterDiscoverySettings
    {
        public long LeaderEntryTTL;

        public long RetryDelay { get; set; }
    }
}