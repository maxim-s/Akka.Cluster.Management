using System;
using System.Threading.Tasks;
using Akka.Cluster.Management.SeedList;

namespace Akka.Cluster.Management
{
    public interface IServiceDiscoveryClient
    {
        Task<EtcdResponse> Get(string key, bool recursive, bool sorted);
        Task<EtcdResponse> Create(string seedsPath, string member);
        Task<EtcdResponse> Delete(string addressMapping, bool recursive);
        void Start();
        void SetLeader();
        void CompareAndSet(string leaderPath, string address, TimeSpan leaderEntryTtl);
    }
}