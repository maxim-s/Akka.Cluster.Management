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

            var stash = Context.CreateStash(this.GetType());

            When(SeedListState.AwaitingInitialState, @event =>
            {
                var fsmEvent = @event.FsmEvent as InitialState;
                if (fsmEvent != null)
                {
                    // TODO: etcd(_.get(settings.seedsPath, recursive = true, sorted = false))
                    return GoTo(SeedListState.AwaitingRegisteredSeeds).Using(new AwaitingRegisteredSeedsData(fsmEvent.Members));
                }
                if (@event.FsmEvent is MemberAdded || @event.FsmEvent is MemberRemoved)
                {
                    stash.Stash();
                    return Stay();
                }
                return null;
            });

            When(SeedListState.AwaitingCommand, @event =>
            {
                var memberAdded = @event.FsmEvent as MemberAdded;
                var commandData = @event.StateData as AwaitingCommandData;
                if (memberAdded != null && commandData != null)
                {
                    // TODO: Implement adding new seedpath to service dicsovery client. example: etcd(_.create(settings.seedsPath, address))
                    return
                        GoTo(SeedListState.AwaitingEtcdReply)
                            .Using(new AwaitingReplyData(memberAdded, commandData.AddressMapping));
                }

                var memberRemoved = @event.FsmEvent as MemberRemoved;
                commandData = @event.StateData as AwaitingCommandData;
                if (memberRemoved != null && commandData != null)
                {
                    string addressMapping;
                    if (commandData.AddressMapping.TryGetValue(memberRemoved.Member, out addressMapping))
                    {
                        // TODO: Implement deleting seedpath from service dicsovery client. Example: etcd(_.delete(key, recursive = false))
                        return
                            GoTo(SeedListState.AwaitingEtcdReply)
                                .Using(new AwaitingReplyData(memberRemoved, commandData.AddressMapping));
                    }

                    return Stay();
                }

                return null;
            });
        }
    }

    public class InitialState
    {
        public InitialState(IImmutableSet<string> members)
        {
            Members = members;
        }

        public IImmutableSet<string> Members { get; }
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