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
using Moq;
using Xunit;

namespace Akka.Cluster.Management.Tests
{
    public class SeedListActorSpec : FSMSpecBase<SeedListState, ISeedListData>
    {
        private const string Addr1 = "akka.tcp://system@host1:50000";
        private const string Addr2 = "akka.tcp://system@host2:50000";

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
            ServiceDiscoveryClientMock.Setup(s => s.Get(Settings.SeedsPath, It.IsAny<bool>(), It.IsAny<bool>())).Returns(seedsTask.Task);

            var seedList = Init();

            seedList.Tell(new InitialState(ImmutableHashSet<string>.Empty));
            ExpectTransitionTo(SeedListState.AwaitingRegisteredSeeds);

            seedsTask.SetResult(new GetNodesResponse(new Dictionary<string, string>()));
            ExpectTransitionTo(SeedListState.AwaitingCommand);
        }

        [Fact]
        private void It_should_delete_stale_seeds()
        {
            var seedsTask = new TaskCompletionSource<GetNodesResponse>();
            var deleteTask1 = new TaskCompletionSource<Response>();
            var deleteTask2 = new TaskCompletionSource<Response>();
            ServiceDiscoveryClientMock.Setup(s => s.Get(Settings.SeedsPath, It.IsAny<bool>(), It.IsAny<bool>())).Returns(seedsTask.Task);
            ServiceDiscoveryClientMock.Setup(s => s.Delete($"{Settings.SeedsPath}/{131}", It.IsAny<bool>())).Returns(deleteTask1.Task);
            ServiceDiscoveryClientMock.Setup(s => s.Delete($"{Settings.SeedsPath}/{132}", It.IsAny<bool>())).Returns(deleteTask2.Task);

            var seedList = Init();

            seedList.Tell(new InitialState(ImmutableHashSet<string>.Empty));
            ExpectTransitionTo(SeedListState.AwaitingRegisteredSeeds);

            seedsTask.SetResult(new GetNodesResponse(new Dictionary<string, string>()
            {
                {$"{Settings.SeedsPath}/{131}", Addr1 },
                {$"{Settings.SeedsPath}/{132}", Addr2 }
            }));

            ExpectTransitionTo(SeedListState.AwaitingCommand);
            ExpectTransitionTo(SeedListState.AwaitingEtcdReply);

            deleteTask1.SetResult(new DeleteNodeResponse(Addr1));
            deleteTask2.SetResult(new DeleteNodeResponse(Addr2));
            ExpectTransitionTo(SeedListState.AwaitingCommand);
        }

        [Fact]
                    // TODO: fix this test.
        private void it_should_register_initial_seeds()
        {
            var seedsTask = new TaskCompletionSource<GetNodesResponse>();
            var createTask1 = new TaskCompletionSource<CreateNodeResponse>();
            var createTask2 = new TaskCompletionSource<CreateNodeResponse>();
            ServiceDiscoveryClientMock.Setup(s => s.Get(Settings.SeedsPath, It.IsAny<bool>(), It.IsAny<bool>())).Returns(seedsTask.Task);
            ServiceDiscoveryClientMock.Setup(s => s.Create($"{Settings.SeedsPath}/{131}", Addr1, It.IsAny<TimeSpan>())).Returns(createTask1.Task);
            ServiceDiscoveryClientMock.Setup(s => s.Create($"{Settings.SeedsPath}/{132}", Addr2, It.IsAny<TimeSpan>())).Returns(createTask2.Task);

            var seedList = Init();

            seedList.Tell(new InitialState(ImmutableHashSet<string>.Empty.Add(Addr1).Add(Addr2)));
            ExpectTransitionTo(SeedListState.AwaitingRegisteredSeeds);

            seedsTask.SetResult(new GetNodesResponse(new Dictionary<string, string>()
            {
                //{$"{Settings.SeedsPath}/{131}", Addr1 },
                //{$"{Settings.SeedsPath}/{132}", Addr2 }
            }));
            ExpectTransitionTo(SeedListState.AwaitingCommand);
            ExpectTransitionTo(SeedListState.AwaitingEtcdReply);

            createTask1.SetResult(new CreateNodeResponse(Addr1));
            createTask2.SetResult(new CreateNodeResponse(Addr2));
            ExpectTransitionTo(SeedListState.AwaitingCommand);
        }
    }
}
