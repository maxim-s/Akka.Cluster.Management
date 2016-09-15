namespace Akka.Cluster.Management
{
    public interface IServiceDiscoveryClient
    {
        void Start();
        void SetLeader();
    }
}