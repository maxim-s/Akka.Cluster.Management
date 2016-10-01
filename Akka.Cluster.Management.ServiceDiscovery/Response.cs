namespace Akka.Cluster.Management.ServiceDiscovery
{
    public class Response
    {
        public string Key { get; set; }

        public Node Node { get; set; }

        public Node PrevNode { get; set; }
    }
}