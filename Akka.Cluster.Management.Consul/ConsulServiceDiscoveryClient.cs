using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        public Task<GetNodesResponse> Get(string key, bool recursive, bool sorted)
        {
            return _consul.KV.List(key).ContinueWith(task =>
            {
                var nodes = task.Result.Response.ToDictionary(pair => pair.Key, pair => Convert(pair.Value));
                return new GetNodesResponse(nodes);
            });
        }

        public Task<CreateNodeResponse> Create(string seedsPath, string member, TimeSpan? ttl = null)
        {
            return _consul.Session.Create(new SessionEntry() {TTL = ttl}).ContinueWith(task =>
            {
                if (task.Result.StatusCode == HttpStatusCode.Created)
                {
                    var distributedLock = _consul.AcquireLock(new LockOptions(member) {Session = task.Result.Response});
                    if (distributedLock.Result.IsHeld)
                    {
                        return new CreateNodeResponse(member);
                    }
                    else
                    {
                        new CreateNodeResponse(string.Empty)
                        {
                            Key = member,
                            Success = false,
                            Reason = "Can't acquire lock"
                        };
                    }
                }
                return new CreateNodeResponse(string.Empty) {Key = member, Success = false, Reason = task.Result.Response} ;
            });
            
        }

        public Task<DeleteNodeResponse> Delete(string key, bool recursive)
        {
            if (recursive)
            {
                _consul.Session.Destroy(key).ContinueWith(task =>
                {
                    if (task.Result.Response)
                    {
                        return new DeleteNodeResponse(string.Empty);
                    }
                    return new DeleteNodeResponse(string.Empty) {Success = false, Reason = "Can not delete."};
                });
            }
            return null;
        }

        public void Start(string path, TimeSpan? ttl = null)
        {
            _consul.KV.Put(new KVPair(path));
        }

        public void SetLeader()
        {
            throw new NotImplementedException();
        }

        public void CompareAndSet(string leaderPath, string address, TimeSpan leaderEntryTtl)
        {
            throw new NotImplementedException();
        }

        private string Convert(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        private byte[] Convert(string value)
        {
            return System.Text.Encoding.UTF8.GetBytes(value);
        }
    }
}
