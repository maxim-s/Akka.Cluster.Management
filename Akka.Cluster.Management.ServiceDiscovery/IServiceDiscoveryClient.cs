using System;
using System.Threading.Tasks;

namespace Akka.Cluster.Management.ServiceDiscovery
{
    public interface IServiceDiscoveryClient
    {
        Task<GetNodesResponse> Get(string key, bool recursive, bool sorted);
        Task<CreateNodeResponse> Create(string seedsPath, string member, TimeSpan? ttl =null);
        Task<Response> Delete(string key, bool recursive);
        void Start(string basePath,TimeSpan? ttl = null);
        void SetLeader();
        void CompareAndSet(string leaderPath, string address, TimeSpan leaderEntryTtl);
    }
}