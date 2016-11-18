using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Management.ServiceDiscovery;
using Akka.Dispatch;
using Akka.Event;

namespace Akka.Cluster.Management.SeedList
{
    public class SeedListActor : FSM<SeedListState, ISeedListData>, IWithUnboundedStash
    {
        private readonly IServiceDiscoveryClient _client;
        private readonly ClusterDiscoverySettings _settings;
        private readonly ILoggingAdapter _log;

        public SeedListActor(IServiceDiscoveryClient client, ClusterDiscoverySettings settings)
        {
            _client = client;
            _settings = settings;
            _log = Context.GetLogger();

            Stash = Context.CreateStash(this.GetType());

            When(SeedListState.AwaitingInitialState, @event =>
            {
                var fsmEvent = @event.FsmEvent as InitialState;
                if (fsmEvent != null)
                {
                    ServiceDiscovery(cl => cl.Get(_settings.SeedsPath));
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
                var state = @event.StateData as AwaitingRegisteredSeedsData;
                var getNodeResponse = @event.FsmEvent as GetNodesResponse;
                if  (getNodeResponse != null && getNodeResponse.Success)
                {
                    var seeds = getNodeResponse.Nodes;
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

                var error = @event.FsmEvent as ServiceDiscovery.Error;
                if (error != null && error.ErrorType == ErrorType.KeyNotFound)
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

                if (error != null && state != null)
                {
                    // TODO: add reason to log
                    _log.Warning("Service discovery error while fetching error while fetching registered seeds");
                    RetryMessage(new InitialState(state.Members));
                    return GoTo(SeedListState.AwaitingInitialState)
                            .Using(new AwaitingInitialStateData());
                }

                var failure = @event.FsmEvent as Status.Failure;
                if (failure != null && state != null)
                {
                    // TODO: add reason to log
                    _log.Warning("Service discovery error while fetching error while fetching registered seeds");
                    RetryMessage(new InitialState(state.Members));
                    Stash.UnstashAll();
                    return GoTo(SeedListState.AwaitingInitialState)
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
                    ServiceDiscovery(cl => cl.Create(_settings.SeedsPath, memberAdded.Member));
                    return
                        GoTo(SeedListState.AwaitingReply)
                            .Using(new AwaitingReplyData(memberAdded, commandData.AddressMapping));
                }

                var memberRemoved = @event.FsmEvent as MemberRemoved;
                commandData = @event.StateData as AwaitingCommandData;
                if (memberRemoved != null && commandData != null)
                {
                    string addressMapping;
                    if (commandData.AddressMapping.TryGetValue(memberRemoved.Member, out addressMapping))
                    {
                        ServiceDiscovery(cl => cl.Delete(_settings.SeedsPath, addressMapping, false));
                        return
                            GoTo(SeedListState.AwaitingReply)
                                .Using(new AwaitingReplyData(memberRemoved, commandData.AddressMapping));
                    }

                    return Stay();
                }

                return null;
            });

            When(SeedListState.AwaitingReply, @event =>
            {
                var state = @event.StateData as AwaitingReplyData;
                var createNodeResponse = @event.FsmEvent as CreateNodeResponse;
                if (createNodeResponse != null)
                {
                    var resp = createNodeResponse;
                    Stash.UnstashAll();
                    return
                        GoTo(SeedListState.AwaitingCommand)
                            .Using(
                                new AwaitingCommandData(
                                    state.AdressMapping.ToImmutableDictionary()
                                        .Add(resp.Address, resp.Key)));
                }

                var deleteNodeResponse = @event.FsmEvent as DeleteNodeResponse;
                if (deleteNodeResponse != null)
                {
                    var resp = deleteNodeResponse;
                    Stash.UnstashAll();
                        return
                            GoTo(SeedListState.AwaitingCommand)
                                .Using(
                                    new AwaitingCommandData(
                                        state.AdressMapping.ToImmutableDictionary()
                                            .Remove(resp.Address)));
                    
                }

                var error = @event.FsmEvent as ServiceDiscovery.Error;
                if (error != null && state != null)
                {
                    // TODO: _log warning 
                    RetryMessage(state.Command);
                    Stash.UnstashAll();
                    return GoTo(SeedListState.AwaitingCommand)
                            .Using(new AwaitingCommandData(state.AdressMapping));
                }

                var failure = @event.FsmEvent as Failure;
                if (failure != null && state != null)
                {
                    // TODO: _log warning 
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