using System;
using System.Threading.Tasks;

namespace Akka.Cluster.Management.ServiceDiscovery
{
    public interface IServiceDiscoveryClient
    {
        Task<Response> Get(string key, bool recursive, bool sorted);
        Task<Response> Create(string seedsPath, string member);
        Task<Response> Delete(string key, bool recursive);
        void Start();
        void SetLeader();
        void CompareAndSet(string leaderPath, string address, TimeSpan leaderEntryTtl);
    }
}