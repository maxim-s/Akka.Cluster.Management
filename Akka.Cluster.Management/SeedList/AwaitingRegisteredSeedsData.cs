using System.Collections.Immutable;

namespace Akka.Cluster.Management.SeedList
{
    class AwaitingRegisteredSeedsData : ISeedListData
    {
        public IImmutableSet<string> Members { get; set; }

        public AwaitingRegisteredSeedsData(IImmutableSet<string> members)
        {
            Members = members;
        }
    }
}