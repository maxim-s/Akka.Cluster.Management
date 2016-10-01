using System.Collections.Immutable;

namespace Akka.Cluster.Management.ServiceDiscovery
{
    public class Node
    {
        public string Address { get; set; }

        public string Value { get; set; }

        public ImmutableList<Node> Nodes { get; set; }

        public string Key { get; set; }
    }
}