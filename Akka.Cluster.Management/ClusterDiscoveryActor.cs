using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Management.LeaderEntry;
using Akka.Cluster.Management.SeedList;
using Akka.Cluster.Management.ServiceDiscovery;
using Akka.Event;
using Akka.Util.Internal;

namespace Akka.Cluster.Management
{
    public class ClusterDiscoveryActor : FSM<ClusterDiscoveryActorState, IImmutableSet<Address>>
    {
        private readonly IServiceDiscoveryClient _client;
        private readonly Cluster _cluster;
        private readonly ILoggingAdapter _log;
        private readonly IActorRef _seedList;
        private readonly ClusterDiscoverySettings _settings;

        public ClusterDiscoveryActor(IServiceDiscoveryClient client, Cluster cluster, ClusterDiscoverySettings settings)
        {
            _client = client;
            _cluster = cluster;
            _settings = settings;
            _log = Context.GetLogger();
            _seedList = Context.ActorOf(Props.Create(() => new SeedListActor(client, settings)));
            When(ClusterDiscoveryActorState.Initial, @event =>
            {
                if (@event.FsmEvent is Start)
                {
                    _client.Start(settings.BasePath);
                    return Stay();
                }
                if (@event.FsmEvent is Response)
                {
                    return GoTo(ClusterDiscoveryActorState.Election);
                }
                return null;
            });

            OnTransition((state, nextState) =>
            {
                if (nextState == ClusterDiscoveryActorState.Election)
                {
                    _log.Info("starting election");
                    ElectionBid();
                }
                if (nextState == ClusterDiscoveryActorState.Leader)
                {
                    // bootstrap the cluster
                    _log.Info("assuming Leader role");
                    _cluster.Join(_cluster.SelfAddress);
                    _cluster.Subscribe(Self, ClusterEvent.SubscriptionInitialStateMode.InitialStateAsSnapshot,
                        typeof (ClusterEvent.IMemberEvent));
                    Context.ActorOf(
                        Props.Create(() => new LeaderEntryActor(_cluster.SelfAddress.ToString(), _client, _settings)));
                }
                if (nextState == ClusterDiscoveryActorState.Follower)
                {
                    // bootstrap the cluster
                    _log.Info("assuming Follower  role");
                    _cluster.Subscribe(Self, ClusterEvent.SubscriptionInitialStateMode.InitialStateAsSnapshot,
                        typeof (ClusterEvent.IClusterDomainEvent));
                    SetTimer("seedsFetch", new SeedsFetchTimeout(), _settings.SeedsFetchTimeout);
                    FetchSeeds();
                }
            });


            When(ClusterDiscoveryActorState.Election, @event =>
            {
                if (@event.FsmEvent is SetLeaderResponse)
                {
                    var resp = (SetLeaderResponse) @event.FsmEvent;
                    if (resp.Success)
                    {
                        return GoTo(ClusterDiscoveryActorState.Leader);
                    }
                    return GoTo(ClusterDiscoveryActorState.Follower);
                }
                if (@event.FsmEvent is Retry)
                {
                    _log.Warning("retrying");
                    ElectionBid();
                    return Stay();
                }
                if (@event.FsmEvent is Status.Failure)
                {
                    var failure = (Status.Failure) @event.FsmEvent;
                    _log.Error(failure.Cause, "Election error");
                    SetTimer("retry", new Retry(), _settings.RetryDelay, false);
                    return Stay();
                }
                _log.Warning("Election error");
                SetTimer("retry", new Retry(), _settings.RetryDelay, false);
                return Stay();
            });


            When(ClusterDiscoveryActorState.Follower, @event =>
            {
                if (@event.FsmEvent is GetNodesResponse)
                {
                    var getNodes = (GetNodesResponse) @event.FsmEvent;
                    var seeds = new List<Address>();
                    if (getNodes.Success)
                    {
                        getNodes.Nodes.ForEach(node =>
                        {
                            Address addr;
                            if (ActorPath.TryParseAddress(node.Value, out addr))
                            {
                                seeds.Add(addr);
                            }
                        });

                        _log.Info(string.Format("attempting to join {0}", seeds));
                        _cluster.JoinSeedNodes(seeds);
                        CancelTimer("seedsFetch");
                        SetTimer("seedsJoin", new JoinTimeout(), _settings.SeedsJoinTimeout, false);
                        return Stay();
                    }
                    SetTimer("retry", new Retry(), _settings.RetryDelay, false);
                    return Stay();
                }
                if (@event.FsmEvent is Retry)
                {
                    FetchSeeds();
                    return Stay();
                }
                if (@event.FsmEvent is SeedsFetchTimeout)
                {
                    _log.Info(string.Format("failed to fetch seed node information in {0} ms",
                        _settings.SeedsFetchTimeout.TotalMilliseconds));
                    return GoTo(ClusterDiscoveryActorState.Election);
                }
                if (@event.FsmEvent is JoinTimeout)
                {
                    _log.Info(string.Format("seed nodes failed to respond in {0} ms",
                        _settings.SeedsJoinTimeout.TotalMilliseconds));
                    return GoTo(ClusterDiscoveryActorState.Election);
                }
                if (@event.FsmEvent is ClusterEvent.LeaderChanged)
                {
                    var leader = (ClusterEvent.LeaderChanged) @event.FsmEvent;
                    if (leader.Leader == _cluster.SelfAddress)
                    {
                        return GoTo(ClusterDiscoveryActorState.Leader);
                    }
                    _log.Info("seen leader change to {0}", leader.Leader);
                    return Stay();
                }
                if (@event.FsmEvent is ClusterEvent.MemberUp)
                {
                    var up = (ClusterEvent.MemberUp) @event.FsmEvent;
                    if (up.Member.Address == cluster.SelfAddress)
                    {
                        _log.Info("joined the cluster");
                        CancelTimer("seedsFetch");
                        CancelTimer("seedsJoin");
                        return Stay();
                    }
                }
                return Stay();
            });


            When(ClusterDiscoveryActorState.Leader, @event =>
            {
                if (@event.FsmEvent is ClusterEvent.CurrentClusterState)
                {
                    var currentClusterState = (ClusterEvent.CurrentClusterState) @event.FsmEvent;
                    _seedList.Tell(
                        new InitialState(
                            currentClusterState.Members.Select(m => m.Address.ToString()).ToImmutableSortedSet()));
                    return Stay().Using(currentClusterState.Members.Select(m => m.Address).ToImmutableSortedSet());
                }

                if (@event.FsmEvent is ClusterEvent.MemberUp)
                {
                    var memberUp = (ClusterEvent.MemberUp) @event.FsmEvent;

                    _seedList.Tell(new MemberAdded(memberUp.Member.Address.ToString()));
                    return Stay().Using(@event.StateData.Add(memberUp.Member.Address));
                }

                if (@event.FsmEvent is ClusterEvent.MemberExited)
                {
                    var memberExited = (ClusterEvent.MemberExited) @event.FsmEvent;

                    _seedList.Tell(new MemberRemoved(memberExited.Member.Address.ToString()));
                    return Stay().Using(@event.StateData.Remove(memberExited.Member.Address));
                }

                if (@event.FsmEvent is ClusterEvent.MemberRemoved)
                {
                    var memberRemoved = (ClusterEvent.MemberRemoved) @event.FsmEvent;
                    if (@event.StateData.Contains(memberRemoved.Member.Address))
                    {
                        _seedList.Tell(new MemberRemoved(memberRemoved.Member.Address.ToString()));
                    }
                    return Stay().Using(@event.StateData.Remove(memberRemoved.Member.Address));
                }
                if (@event.FsmEvent is ClusterEvent.IClusterDomainEvent)
                {
                    return Stay();
                }
                return null;
            });

            WhenUnhandled(@event =>
            {
                _log.Warning("unhandled message {0}", @event.FsmEvent);
                return Stay();
            });
            StartWith(ClusterDiscoveryActorState.Initial, ImmutableHashSet<Address>.Empty);
            Initialize();
        }

        private void FetchSeeds()
        {
            _client.Get(_settings.SeedsPath).PipeTo(Self);
        }

        private void ElectionBid()
        {
            _client.SetLeader(_settings.LeaderPath, _cluster.SelfAddress.ToString(), _settings.LeaderEntryTTL,
                false).PipeTo(Self);
        }

        private class JoinTimeout
        {
        }

        private class SeedsFetchTimeout
        {
        }
    }
}