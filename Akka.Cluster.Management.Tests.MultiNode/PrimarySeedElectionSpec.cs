using System;
using System.Diagnostics;
using System.Threading;
using Akka.Actor.Internal;
using Akka.Cluster.Management.Consul;
using Akka.Cluster.Management.ServiceDiscovery;
using Akka.Cluster.TestKit;
using Akka.Configuration;
using Akka.Remote.TestKit;

namespace Akka.Cluster.Management.Tests.MultiNode
{
    public class PrimarySeedElectionMultiNodeConfig : MultiNodeConfig
    {
        private readonly RoleName _first;
        private readonly RoleName _second;
        private readonly RoleName _third;

        public PrimarySeedElectionMultiNodeConfig()
        {
            _first = Role("first");
            _second = Role("second");
            _third = Role("third");
            CommonConfig = DebugConfig(false)
                .WithFallback(ConfigurationFactory.ParseString(@"                     
                akka.cluster.discovery.timeouts.retry = 500 ms
                akka.cluster.discovery.leaderPath = ""leader""
                akka.cluster.discovery.seedsPath = ""seedsPath""
                akka.loglevel = DEBUG")).WithFallback(MultiNodeClusterSpec.ClusterConfig());
        }
    }

    public class PrimarySeedElectionMultiJvmNode1 : PrimarySeedElectionSpec
    {
    }

    public class PrimarySeedElectionMultiJvmNode2 : PrimarySeedElectionSpec
    {
    }

    public class PrimarySeedElectionMultiJvmNode3 : PrimarySeedElectionSpec
    {
    }

    public abstract class PrimarySeedElectionSpec : MultiNodeClusterSpec
    {
        private readonly PrimarySeedElectionMultiNodeConfig _config;

        protected PrimarySeedElectionSpec() : this(new PrimarySeedElectionMultiNodeConfig())
        {
        }

        protected PrimarySeedElectionSpec(PrimarySeedElectionMultiNodeConfig config) : base(config)
        {
            _config = config;
        }

        [MultiNodeFact]
        public void ClusterDiscoveryExtension_should_bootstrap_a_cluster()
        {
            var discoverySettings = ClusterDiscoverySettings.Load(Sys.Settings.Config);
            IServiceDiscoveryClient client = new ConsulServiceDiscoveryClient();
            Cluster.Get(Sys).Subscribe(TestActor, typeof (ClusterEvent.MemberUp));
            ExpectMsg<ClusterEvent.CurrentClusterState>();
            Thread.Sleep(1000);
            //Debugger.Launch();
            new ClusterDiscovery((ActorSystemImpl) Sys).Start();
            ExpectMsg<ClusterEvent.MemberUp>(TimeSpan.FromSeconds(10));
            ExpectMsg<ClusterEvent.MemberUp>(TimeSpan.FromSeconds(10));
            ExpectMsg<ClusterEvent.MemberUp>(TimeSpan.FromSeconds(10));
        }
    }
}