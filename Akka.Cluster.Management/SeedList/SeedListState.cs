namespace Akka.Cluster.Management.SeedList
{
    public enum SeedListState
    {
        AwaitingInitialState,
        AwaitingRegisteredSeeds,
        AwaitingCommand,
        AwaitingEtcdReply
    }
}