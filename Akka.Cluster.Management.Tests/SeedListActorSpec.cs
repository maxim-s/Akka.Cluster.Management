using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Management.SeedList;
using Akka.Cluster.Management.ServiceDiscovery;
using Xunit;

namespace Akka.Cluster.Management.Tests
{
    public class SeedListActorSpec : FSMSpecBase<SeedListState, ISeedListData>
    {
        private IActorRef Init()
        {
            var seedList = Sys.ActorOf(Props.Create(() => new SeedListActor(ServiceDiscoveryClientMock.Object, Settings)));
            seedList.Tell(new FSMBase.SubscribeTransitionCallBack(StateProbe.Ref));
            ExpectInitialState(SeedListState.AwaitingInitialState);
            return seedList;
        }

        //private Task<GetNodesResponse> FetchSeedsReq()
        //{
        //    return ServiceDiscoveryClientMock.Get(Settings.SeedsPath, true, true);
        //}

        [Fact]
        private void Seed_list_manager_actor_should_proceed_to_AwaitCommand_when_seed_lists_are_empty()
        {
            var seedsTask = new TaskCompletionSource<GetNodesResponse>();
            ServiceDiscoveryClientMock.Setup(s => s.Get(Settings.SeedsPath)).Returns(seedsTask.Task);

            var seedList = Init();

            seedList.Tell(new InitialState(ImmutableHashSet<string>.Empty));
            ExpectTransitionTo(SeedListState.AwaitingRegisteredSeeds);

            seedsTask.SetResult(new GetNodesResponse(new Dictionary<string, string>()));
            ExpectTransitionTo(SeedListState.AwaitingCommand);
        }
    }
}
