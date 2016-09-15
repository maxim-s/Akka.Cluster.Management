namespace Akka.Cluster.Management
{
    public enum ClusterDiscoveryActorState
    {
        Initial,
        Election,
        Leader,
        Follower
    }
}