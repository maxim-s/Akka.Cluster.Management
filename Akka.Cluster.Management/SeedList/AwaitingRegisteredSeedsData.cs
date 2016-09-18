using System.Collections.Immutable;

namespace Akka.Cluster.Management.SeedList
{
    class AwaitingRegisteredSeedsData : ISeedListData
    {
        public IImmutableSet<string> CurrentSeeds { get; }

        public AwaitingRegisteredSeedsData(IImmutableSet<string> currentSeeds)
        {
            CurrentSeeds = currentSeeds;
        }
    }
}