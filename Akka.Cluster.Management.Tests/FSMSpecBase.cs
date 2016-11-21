using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Cluster.Management.ServiceDiscovery;
using Akka.TestKit;
using Moq;

namespace Akka.Cluster.Management.Tests
{
    public abstract class FSMSpecBase<State, Data> : TestKit.Xunit2.TestKit
    {
        private TimeSpan TransitionTimeout;


        protected Mock<IServiceDiscoveryClient> ServiceDiscoveryClientMock { get; private set; }

        protected FSMSpecBase() 
        {
            // TODO: Config settings
            Settings = new ClusterDiscoverySettings(string.Empty, string.Empty, TimeSpan.MaxValue, TimeSpan.MaxValue,
                TimeSpan.MaxValue, TimeSpan.MaxValue, string.Empty, 0, string.Empty);
            StateProbe = CreateTestProbe(Sys);
            TransitionTimeout = TimeSpan.FromSeconds(10);

            ServiceDiscoveryClientMock = new Mock<IServiceDiscoveryClient>();
        }

        public TestProbe StateProbe { get; set; }
        public ClusterDiscoverySettings Settings { get; set; }

        public void ExpectTransitionTo(State expState)
        {
            var val = StateProbe.ExpectMsg<FSMBase.Transition<State>>(TransitionTimeout);
            Assertions.AssertTrue(EqualityComparer<State>.Default.Equals(val.To, expState));
        }

        public void ExpectInitialState(State expState)
        {
            var val = StateProbe.ExpectMsg<FSMBase.CurrentState<State>>(TransitionTimeout);
            Assertions.AssertTrue(EqualityComparer<State>.Default.Equals(val.State, expState));
        }

        protected override void AfterAll()
        {
            Shutdown(Sys);
        }
    }
}
