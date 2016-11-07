using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Management.ServiceDiscovery;
using Akka.Dispatch;

namespace Akka.Cluster.Management.SeedList
{
    public class SeedListActor : FSM<SeedListState, ISeedListData>, IWithUnboundedStash
    {
        private readonly IServiceDiscoveryClient _client;
        private readonly ClusterDiscoverySettings _settings;

        public SeedListActor(IServiceDiscoveryClient client, ClusterDiscoverySettings settings)
        {
            _client = client;
            _settings = settings;

            Stash = Context.CreateStash(this.GetType());

            When(SeedListState.AwaitingInitialState, @event =>
            {
                var fsmEvent = @event.FsmEvent as InitialState;
                if (fsmEvent != null)
                {
                    ServiceDiscovery(cl => cl.Get(_settings.SeedsPath, true, false));
                    return GoTo(SeedListState.AwaitingRegisteredSeeds).Using(new AwaitingRegisteredSeedsData(fsmEvent.Members));
                }
                if (@event.FsmEvent is MemberAdded || @event.FsmEvent is MemberRemoved)
                {
                    Stash.Stash();
                    return Stay();
                }
                return null;
            });

            When(SeedListState.AwaitingRegisteredSeeds, @event =>
            {
                var fsmEvent = @event.FsmEvent as Response;
                var state = @event.StateData as AwaitingRegisteredSeedsData;
                if  (@event.FsmEvent is GetNodesResponse)
                {
                    var resp = @event.FsmEvent as GetNodesResponse;
                    var seeds = resp.Nodes;
                    if (seeds == null)
                    {
                        Stash.UnstashAll();
                        return
                            GoTo(SeedListState.AwaitingCommand)
                                .Using(new AwaitingCommandData(ImmutableDictionary<string, string>.Empty));
                    }
                    else
                    {
                        var currentSeeds = state.Members;
                        var registeredSeeds = seeds.Select(v => v.Value).ToImmutableHashSet();

                        foreach (var member in currentSeeds.Except(registeredSeeds))
                        {
                            Self.Tell(new MemberAdded(member));
                        }

                        foreach (var member in registeredSeeds.Except(currentSeeds))
                        {
                            Self.Tell(new MemberRemoved(member));
                        }

                        var addressMapping = seeds.ToDictionary(node => node.Value, node => node.Key);
                        Stash.UnstashAll();
                        return GoTo(SeedListState.AwaitingCommand).Using(new AwaitingCommandData(addressMapping));
                    }
                }

                var etcdError = @event.FsmEvent as ServiceDiscovery.Error;
                if (etcdError != null && etcdError.ErrorType == ErrorType.KeyNotFound)
                {
                    foreach (var member in state.Members)
                    {
                        Self.Tell(new MemberAdded(member));
                    }

                    Stash.UnstashAll();
                    return
                        GoTo(SeedListState.AwaitingCommand)
                            .Using(new AwaitingCommandData(new Dictionary<string, string>()));
                }

                if (etcdError != null && state != null)
                {
                    // TODO: Log warning 
                    RetryMessage(new InitialState(state.Members));
                    return GoTo(SeedListState.AwaitingCommand)
                            .Using(new AwaitingInitialStateData());
                }

                var failure = @event.FsmEvent as Failure;
                if (failure != null && state != null)
                {
                    // TODO: Log warning 
                    RetryMessage(new InitialState(state.Members));
                    Stash.UnstashAll();
                    return GoTo(SeedListState.AwaitingCommand)
                            .Using(new AwaitingInitialStateData());
                }

                if (@event.FsmEvent is MemberAdded || @event.FsmEvent is MemberRemoved)
                {
                    Stash.Stash();
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
                    ServiceDiscovery(cl => cl.Create(_settings.SeedsPath, memberAdded.Member)).Start();
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
                        ServiceDiscovery(cl => cl.Delete(addressMapping, false)).Start();
                        // TODO: Implement deleting seedpath from service dicsovery client. Example: etcd(_.delete(key, recursive = false))
                        return
                            GoTo(SeedListState.AwaitingEtcdReply)
                                .Using(new AwaitingReplyData(memberRemoved, commandData.AddressMapping));
                    }

                    return Stay();
                }

                return null;
            });

            When(SeedListState.AwaitingEtcdReply, @event =>
            {
                var state = @event.StateData as AwaitingReplyData;
                if (@event.FsmEvent is CreateNodeResponse)
                {
                    var resp = @event.FsmEvent as CreateNodeResponse;
                    Stash.UnstashAll();
                    return
                        GoTo(SeedListState.AwaitingCommand)
                            .Using(
                                new AwaitingCommandData(
                                    state.AdressMapping.ToImmutableDictionary()
                                        .Add(resp.Address, resp.Key)));
                }

                if (@event.FsmEvent is DeleteNodeResponse)
                    {
                    var resp = @event.FsmEvent as DeleteNodeResponse;
                    Stash.UnstashAll();
                        return
                            GoTo(SeedListState.AwaitingCommand)
                                .Using(
                                    new AwaitingCommandData(
                                        state.AdressMapping.ToImmutableDictionary()
                                            .Remove(resp.Address)));
                    
                }

                var etcdError = @event.FsmEvent as ServiceDiscovery.Error;
                if (etcdError != null && state != null)
                {
                    // TODO: Log warning 
                    RetryMessage(state.Command);
                    Stash.UnstashAll();
                    return GoTo(SeedListState.AwaitingCommand)
                            .Using(new AwaitingCommandData(state.AdressMapping));
                }

                var failure = @event.FsmEvent as Failure;
                if (failure != null && state != null)
                {
                    // TODO: Log warning 
                    RetryMessage(state.Command);
                    Stash.UnstashAll();
                    return GoTo(SeedListState.AwaitingCommand)
                            .Using(new AwaitingCommandData(state.AdressMapping));
                }

                if (@event.FsmEvent is MemberAdded || @event.FsmEvent is MemberRemoved)
                {
                    Stash.Stash();
                    return Stay();
                }

                return null;
            });

            StartWith(SeedListState.AwaitingInitialState, new AwaitingInitialStateData());
            Initialize();
        }

        private Task ServiceDiscovery<T>(Func<IServiceDiscoveryClient, Task<T>> operation) where T: Response
        {
            return operation(this._client)
                .PipeTo(Self);
        }

        private void RetryMessage(object msg)
        {
            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(_settings.RetryDelay.TotalMilliseconds), Self, msg, Self);
        }

        public IStash Stash { get; set; }
    }

    public class Failure
    {
        
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