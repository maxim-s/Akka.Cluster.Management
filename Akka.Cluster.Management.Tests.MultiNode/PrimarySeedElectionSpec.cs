using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                akka.cluster.discovery.etcd.timeouts.etcdRetry = 500 ms
                akka.loglevel = INFO")).WithFallback(MultiNodeClusterSpec.ClusterConfig());

        }
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
            //MultiNodeSpecBeforeAll();
            //var discoverySettings = ClusterDiscoverySettings.Load(Sys.Settings.Config);
            //var httpClientSettings = new ClientConnectionSettings(Sys).WithConnectingTimeout(discoverySettings.ConnectionTimeout)
            //  .WithIdleTimeout(discoverySettings.RequestTimeout)

            //IServiceDiscoveryClient client = new ConsulServiceDiscoveryClient();

            //Await.ready(etcd.delete("/akka", recursive = true), 3.seconds)

        }

    }

}
