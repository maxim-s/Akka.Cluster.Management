using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Management.SeedList;
using Akka.Cluster.Management.ServiceDiscovery;
using Akka.Event;

namespace Akka.Cluster.Management
{
    public class ClusterDiscoveryActor : FSM<ClusterDiscoveryActorState, IImmutableSet<Address>>
    {
        private readonly IServiceDiscoveryClient _client;
        private readonly Cluster _cluster;
        private readonly ClusterDiscoverySettings _settings;
        private readonly IActorRef _seedList;
        private readonly ILoggingAdapter _log;

        public ClusterDiscoveryActor(IServiceDiscoveryClient client, Cluster cluster, ClusterDiscoverySettings settings)
        {
            _client = client;
            _cluster = cluster;
            _settings = settings;
            _log = Context.GetLogger();
            _seedList = Context.ActorOf(Props.Create(() => new SeedListActor(client, settings)));
            When(ClusterDiscoveryActorState.Initial, @event=>
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
            } );

            OnTransition((state, nextState) =>
            {
                if (nextState == ClusterDiscoveryActorState.Election)
                {
                    _log.Info("starting election");
                    ElectionBid();
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
                }
                if (@event.FsmEvent is Error)
                {
                    return GoTo(ClusterDiscoveryActorState.Follower);
                }
                return null;
            });

  //          when(Election) {
  //  case Event(_: EtcdResponse, _) ⇒
  //    goto(Leader)
  //  case Event(EtcdError(EtcdError.NodeExist, _, _, _), _) ⇒
  //    goto(Follower)
  //  case Event(err @ EtcdError, _) ⇒
  //    log.error(s"Election error: $err")
  //    setTimer("retry", Retry, settings.etcdRetryDelay, false)
  //    stay()
  //  case Event(Status.Failure(t), _) ⇒
  //    log.error(t, "Election error")
  //    setTimer("retry", Retry, settings.etcdRetryDelay, false)
  //    stay()
  //  case Event(Retry, _) ⇒
  //    log.warning("retrying")
  //    electionBid()
  //    stay()
  //}

            When(ClusterDiscoveryActorState.Leader, @event =>
            {
                if (@event.FsmEvent is ClusterEvent.CurrentClusterState)
                {
                    var currentClusterState = (ClusterEvent.CurrentClusterState) @event.FsmEvent;
                    _seedList.Tell(new InitialState(currentClusterState.Members.Select(m=> m.Address.ToString()).ToImmutableSortedSet()));
                    return Stay().Using(currentClusterState.Members.Select(m => m.Address).ToImmutableSortedSet());
                }

                if (@event.FsmEvent is ClusterEvent.MemberUp)
                {
                    var memberUp = (ClusterEvent.MemberUp)@event.FsmEvent;
                    
                    _seedList.Tell(new MemberAdded(memberUp.Member.Address.ToString()));
                    return Stay().Using(@event.StateData.Add(memberUp.Member.Address));
                }

                if (@event.FsmEvent is ClusterEvent.MemberExited)
                {
                    var memberExited = (ClusterEvent.MemberExited)@event.FsmEvent;

                    _seedList.Tell(new MemberRemoved(memberExited.Member.Address.ToString()));
                    return Stay().Using(@event.StateData.Remove(memberExited.Member.Address));
                }

                if (@event.FsmEvent is ClusterEvent.MemberRemoved)
                {
                    var memberRemoved = (ClusterEvent.MemberRemoved)@event.FsmEvent;
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
            StartWith(ClusterDiscoveryActorState.Initial, ImmutableHashSet<Address>.Empty);
            Initialize();
        }

        private void ElectionBid()
        {
            _client.SetLeader(_settings.LeaderPath, _cluster.SelfAddress.ToString(), _settings.LeaderEntryTTL, false)
                .PipeTo(Self);
        }
    }
}
