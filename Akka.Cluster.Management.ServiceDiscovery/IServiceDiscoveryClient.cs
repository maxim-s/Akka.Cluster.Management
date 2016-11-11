using System;
using System.Threading.Tasks;

namespace Akka.Cluster.Management.ServiceDiscovery
{
    public interface IServiceDiscoveryClient
    {
        Task<GetNodesResponse> Get(string key);
        Task<CreateNodeResponse> Create(string seedsPath, string member, TimeSpan? ttl =null);
        Task<DeleteNodeResponse> Delete(string seedsPath,string member, bool recursive);
        void Start(string basePath,TimeSpan? ttl = null);
        Task<SetLeaderResponse> SetLeader(string leaderPath, string address, TimeSpan leaderEntryTtl);
    }
}