using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Management.LeaderEntry;
using Akka.Cluster.Management.SeedList;

namespace Akka.Cluster.Management.Tests
{
    public class LeaderEntryActorSpec : FSMSpecBase<LeaderEntryState, LeaderEntryData>
    {
        private const string Addr = "leaderAddress";

        private IActorRef Init()
        {
            return Init(Settings);
        }

        private IActorRef Init(ClusterDiscoverySettings clusterDiscoverySettings)
        {
            var leaderEntryActor = Sys.ActorOf(Props.Create(() => new LeaderEntryActor(Addr, ServiceDiscoveryClientMock.Object, Settings)));
            leaderEntryActor.Tell(new FSMBase.SubscribeTransitionCallBack(StateProbe.Ref));
            ExpectInitialState(LeaderEntryState.Idle);
            return leaderEntryActor;
        }

        private void leader_entry_actor_should_refresh_the_entry_at_TTL_2_periods()
        {
        }
    }
}
