using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Actor;

namespace Akka.Cluster.Management.SeedList
{
    public class SeedListActor : FSM<SeedListState, ISeedListData>
    {
        private readonly IServiceDiscoveryClient _client;
        private readonly ClusterDiscoverySettings _settings;

        public SeedListActor(IServiceDiscoveryClient client, ClusterDiscoverySettings settings)
        {
            _client = client;
            _settings = settings;
        }
    }

    public class InitialState
    {
        public InitialState(IImmutableSet<string> members)
        {
        }
    }

    interface ICommand
    {
        
    }

    public class MemberAdded : ICommand
    {
        public string Member { get; private set; }

        public MemberAdded(string member)
        {
            Member = member;
        }
    }

    public class MemberRemoved : ICommand
    {
        public string Member { get; private set; }

        public MemberRemoved(string member)
        {
            Member = member;
        }
    }
}