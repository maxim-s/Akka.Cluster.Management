using System;

namespace Akka.Cluster.Management
{
    public interface IServiceDiscoveryClient
    {
        void Start();
        void SetLeader();
        void CompareAndSet(string leaderPath, string address, TimeSpan leaderEntryTtl);
    }
}