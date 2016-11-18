using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Management.LeaderEntry;
using Akka.Cluster.Management.SeedList;
using Akka.Cluster.Management.ServiceDiscovery;
using Moq;
using Xunit;
using Xunit.Sdk;

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
            var leaderEntryActor = Sys.ActorOf(Props.Create(() => new LeaderEntryActor(Addr, ServiceDiscoveryClientMock.Object, clusterDiscoverySettings)));
            leaderEntryActor.Tell(new FSMBase.SubscribeTransitionCallBack(StateProbe.Ref));
            ExpectInitialState(LeaderEntryState.Idle);
            return leaderEntryActor;
        }

        [Fact]
        private void leader_entry_actor_should_refresh_the_entry_at_TTL_2_periods()
        {
            var refreshTask1 = new TaskCompletionSource<SetLeaderResponse>();
            var refreshTask2 = new TaskCompletionSource<SetLeaderResponse>();
            ServiceDiscoveryClientMock.SetupSequence(
                s => s.SetLeader(Settings.LeaderPath, Addr, Settings.LeaderEntryTTL))
                .Returns(refreshTask1.Task)
                .Returns(refreshTask2.Task);

            var leaderActor = Init();

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            refreshTask1.SetResult(new SetLeaderResponse(Settings.LeaderPath, Addr));
            ExpectTransitionTo(LeaderEntryState.Idle);

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            refreshTask2.SetResult(new SetLeaderResponse(Settings.LeaderPath, Addr));
            ExpectTransitionTo(LeaderEntryState.Idle);
        }

        [Fact]
        private void it_should_retry_opreations_failed_because_of_service_discovery_client_internal_errors()
        {
            var refreshTask1 = new TaskCompletionSource<SetLeaderResponse>();
            var refreshTask2 = new TaskCompletionSource<SetLeaderResponse>();
            ServiceDiscoveryClientMock.SetupSequence(
                s => s.SetLeader(Settings.LeaderPath, Addr, Settings.LeaderEntryTTL))
                .Returns(refreshTask1.Task)
                .Returns(refreshTask2.Task);

            var leaderActor = Init();

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            refreshTask1.SetResult(new SetLeaderResponse(string.Empty, string.Empty)
            {
                Success = false,
                Reason = "Something wrong"
            });
            ExpectTransitionTo(LeaderEntryState.Idle);

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            refreshTask2.SetResult(new SetLeaderResponse(Settings.LeaderPath, Addr));
            ExpectTransitionTo(LeaderEntryState.Idle);
        }

        //private void it_should_retry_opreations_failed_because_of_transport_level_errors()
        //{
            
        //}
        [Fact(Skip = "now it's not clear how to determinate is it CreateLeaderEntry or RefreshLeaderEntry process. For now skip this test.")]
        private void it_should_attempt_to_recreate_leader_entry_when_it_expires()
        {
            var refreshTask1 = new TaskCompletionSource<SetLeaderResponse>();
            var refreshTask2 = new TaskCompletionSource<SetLeaderResponse>();
            var recreateTask = new TaskCompletionSource<SetLeaderResponse>();
            ServiceDiscoveryClientMock.SetupSequence(
                s => s.SetLeader(Settings.LeaderPath, Addr, Settings.LeaderEntryTTL))
                .Returns(refreshTask1.Task)
                .Returns(refreshTask2.Task);
            ServiceDiscoveryClientMock.Setup(
                s => s.SetLeader(Settings.LeaderPath, Addr, Settings.LeaderEntryTTL))
                .Returns(recreateTask.Task);

            var leaderActor = Init();

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            refreshTask1.SetResult(new SetLeaderResponse(string.Empty, string.Empty) {Success = false});
            ExpectTransitionTo(LeaderEntryState.Idle);

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            refreshTask2.SetResult(new SetLeaderResponse(Settings.LeaderPath, Addr));
            ExpectTransitionTo(LeaderEntryState.Idle);

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            recreateTask.SetResult(new SetLeaderResponse(Settings.LeaderPath, Addr));
            ExpectTransitionTo(LeaderEntryState.Idle);
        }

        [Fact(Skip = "now it's not clear how to determinate is it CreateLeaderEntry or RefreshLeaderEntry process. For now skip this test.")]
        private void it_should_attempt_to_recreate_leader_entry_when_it_is_hijacked_by_another_node()
        {
            var refreshTask1 = new TaskCompletionSource<SetLeaderResponse>();
            var refreshTask2 = new TaskCompletionSource<SetLeaderResponse>();
            var recreateTask = new TaskCompletionSource<SetLeaderResponse>();
            ServiceDiscoveryClientMock.SetupSequence(
                s => s.SetLeader(Settings.LeaderPath, Addr, Settings.LeaderEntryTTL))
                .Returns(refreshTask1.Task)
                .Returns(refreshTask2.Task);
            ServiceDiscoveryClientMock.Setup(
                s => s.SetLeader(Settings.LeaderPath, Addr, Settings.LeaderEntryTTL))
                .Returns(recreateTask.Task);

            var leaderActor = Init();

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            refreshTask1.SetResult(new SetLeaderResponse(string.Empty, string.Empty) { Success = false });
            ExpectTransitionTo(LeaderEntryState.Idle);

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            refreshTask2.SetResult(new SetLeaderResponse(Settings.LeaderPath, Addr));
            ExpectTransitionTo(LeaderEntryState.Idle);

            ExpectTransitionTo(LeaderEntryState.AwaitingReply);
            recreateTask.SetResult(new SetLeaderResponse(Settings.LeaderPath, Addr));
            ExpectTransitionTo(LeaderEntryState.Idle);
        }
    }
}
