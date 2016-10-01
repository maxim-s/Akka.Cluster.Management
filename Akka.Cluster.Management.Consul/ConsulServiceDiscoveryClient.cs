using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Akka.Cluster.Management.ServiceDiscovery;
using Consul;
using Node = Akka.Cluster.Management.ServiceDiscovery.Node;

namespace Akka.Cluster.Management.Consul
{
    public class ConsulServiceDiscoveryClient : IServiceDiscoveryClient
    {
        private readonly ConsulClient _consul;
        public ConsulServiceDiscoveryClient()
        {
            _consul = new ConsulClient();
        }

        public Task<Response> Get(string key, bool recursive, bool sorted)
        {
            KVPairConverter cv = new KVPairConverter();
            
            return _consul.KV.Get(key).ContinueWith(task =>
            {
                var kvPair = task.Result.Response;
                return new Response() {Key = "get", Node = ConvertTo<Node>(kvPair.Value)};
            });
        }

        public Task<Response> Create(string seedsPath, string member)
        {
            return null;
        }

        public Task<Response> Delete(string key, bool recursive)
        {
            if (recursive)
            {
                
            }
            return null;
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void SetLeader()
        {
            throw new NotImplementedException();
        }

        public void CompareAndSet(string leaderPath, string address, TimeSpan leaderEntryTtl)
        {
            throw new NotImplementedException();
        }

        private T ConvertTo<T>(byte[] bytes)
        {
            return default(T);
        }

        private byte[] ConvertToBytes<T>(T value)
        {
            return new byte[]{ 1};
        }
    }
}
