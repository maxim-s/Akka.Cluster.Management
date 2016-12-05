using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Cluster.Management.Consul;
using Akka.Cluster.Management.ServiceDiscovery;

namespace Akka.Cluster.Management
{
    public class ClusterDiscoveryExtension : ExtensionIdProvider<ClusterDiscovery>
    {
        public override ClusterDiscovery CreateExtension(ExtendedActorSystem system)
        {
            return new ClusterDiscovery((ActorSystemImpl) system);
        }
    }

    public class ClusterDiscovery : IExtension
    {
        public ClusterDiscovery(ActorSystemImpl system)
        {
            System = system;
            DiscoverySettings = ClusterDiscoverySettings.Load(System.Settings.Config);
            IServiceDiscoveryClient serviceDiscovery = new ConsulServiceDiscoveryClient();
            var cluster = Cluster.Get(System);
            Discovery =
                System.ActorOf(
                    Props.Create(() => new ClusterDiscoveryActor(serviceDiscovery, cluster, DiscoverySettings)));
        }

        public ActorSystemImpl System { get; }
        public ClusterDiscoverySettings DiscoverySettings { get; }
        public IActorRef Discovery { get; }

        public void Start()
        {
            Discovery.Tell(new Start());
        }
    }
}