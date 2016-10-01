using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Management.SeedList;
using Akka.Cluster.Management.ServiceDiscovery;

namespace Akka.Cluster.Management
{
    public class ClusterDiscoveryActor : FSM<ClusterDiscoveryActorState, IImmutableSet<Address>>
    {
        private readonly IServiceDiscoveryClient _client;
        private readonly IActorRef _seedList;

        public ClusterDiscoveryActor(IServiceDiscoveryClient client, ClusterDiscoverySettings settings)
        {
            _client = client;
            _seedList = Context.ActorOf(Props.Create(() => new SeedListActor(client, settings)));
            When(ClusterDiscoveryActorState.Initial, @event=>
            {
                if (@event.FsmEvent is Start)
                {
                    _client.Start(); 
                    return Stay();
                }
                if (@event.FsmEvent is Election || @event.FsmEvent is Error)
                {
                    return GoTo(ClusterDiscoveryActorState.Election);
                }
                return null;
            } );

            When(ClusterDiscoveryActorState.Election, @event =>
            {
                if (@event.FsmEvent is Election)
                {
                    return GoTo(ClusterDiscoveryActorState.Leader);
                }
                if (@event.FsmEvent is Error)
                {
                    return GoTo(ClusterDiscoveryActorState.Follower);
                }
                return null;
            });

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
        }
    }
}
