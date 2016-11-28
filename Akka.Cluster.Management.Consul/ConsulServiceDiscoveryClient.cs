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
        public ConsulServiceDiscoveryClient(Configuration config = null)
        {
            _consul = config == null
                ? new ConsulClient()
                : new ConsulClient(configuration =>
                {
                    configuration.HttpAuth = config.HttpAuth;
                    configuration.Address = config.Address;
                    configuration.Datacenter = config.Datacenter;
                    configuration.Token = config.Token;
                    configuration.WaitTime = config.WaitTime;
                });
        }

        public Task<GetNodesResponse> Get(string seedsPath)
        {
            return _consul.KV.List(seedsPath).ContinueWith(task =>
            {
                if (task.Result.StatusCode == HttpStatusCode.OK)
                {
                    var nodes = task.Result.Response.ToDictionary(pair => pair.Key, pair => Convert(pair.Value));
                    return new GetNodesResponse(nodes) { Success = true };
                }
                return new GetNodesResponse(null) { Success = false, Reason = string.Format("Failed with status {0}", task.Result.StatusCode) };
            });
        }

        public Task<CreateNodeResponse> Create(string seedsPath, string member, TimeSpan? ttl = null)
        {
            var key = CreateKey(seedsPath, member);
            return _consul.KV.Put(new KVPair(key) { Value = Convert(member) }).ContinueWith(task =>
              {
                  if (task.Result.StatusCode == HttpStatusCode.OK)
                  {
                      return new CreateNodeResponse(string.Empty)
                      {
                          Key = member,
                          Success = true
                      };
                  }
                  else
                  {
                      return new CreateNodeResponse(string.Empty) { Key = member, Success = false, Reason = string.Format("failed to add member {0} with seeds path {1}", member, seedsPath) };
                  }

              });

        }

        public Task<DeleteNodeResponse> Delete(string seedsPath, string member = null, bool recursive = false)
        {
            var key = CreateKey(seedsPath, member);
            var deleteTask = recursive ? _consul.KV.DeleteTree(seedsPath) : _consul.KV.Delete(key);
            return deleteTask.ContinueWith(task =>
            {
                if (task.Result.StatusCode == HttpStatusCode.OK)
                {
                    return new DeleteNodeResponse(key) { Success = true };
                }
                else
                {
                    return new DeleteNodeResponse(key) { Success = false, Reason = "Can't delete  lock" };
                }
            });
        }

        public Task<StartResponse> Start(string path, TimeSpan? ttl = null)
        {
            return _consul.KV.Put(new KVPair(path)).ContinueWith(task =>
            {
                if (!task.IsFaulted && task.Result.StatusCode == HttpStatusCode.OK)
                {
                    return new StartResponse(path);
                }
                else
                {
                    return new StartResponse(path)
                    {
                        Success = false,
                        Reason = string.Format("can't create {0}", path),
                    };
                }
            });
        }

        public Task<NodeExistResponse> NodeExist(string leaderPath, string address)
        {
            return _consul.Session.List().ContinueWith(task =>
            {
                if (task.Result.StatusCode == HttpStatusCode.OK && task.Result.Response != null && task.Result.Response.Any(s=> s.Name == leaderPath))
                {
                    return new NodeExistResponse(leaderPath, address);
                }
                return new NodeExistResponse(leaderPath, address) {Success = false};
            });
        }

        public Task<SetLeaderResponse> SetLeader(string leaderPath, string address, TimeSpan leaderEntryTtl)
        {
            var sessionTask = CreateSession(leaderPath, address, leaderEntryTtl);
            return Acquire(sessionTask);
        }

        private Task<CreateSessionResponse> CreateSession(string leaderPath, string address, TimeSpan leaderEntryTtl)
        {
            return _consul.Session.Create(new SessionEntry() { TTL = leaderEntryTtl, Name = leaderPath }).ContinueWith(task =>
            {
                if (task.Result.StatusCode == HttpStatusCode.OK)
                {
                    return new CreateSessionResponse(task.Result.Response, leaderPath, address, true, null);
                }
                else
                {
                    return new CreateSessionResponse(task.Result.Response, leaderPath, address, false, string.Format("Failed to Create seesion with status {0}", task.Result.StatusCode));
                }
            });
        }

        private Task<SetLeaderResponse> Acquire(Task<CreateSessionResponse> createSessionTask)
        {
            if (createSessionTask.Result.Success)
            {
                var response = createSessionTask.Result;
                return _consul.KV.Acquire(new KVPair(response.LeaderPath)
                {
                    Session = response.Session,
                    Value = Convert(response.Address)
                })
                    .ContinueWith(
                        acq =>
                        {
                            if (acq.Result.StatusCode == HttpStatusCode.OK)
                            {
                                return new SetLeaderResponse(response.LeaderPath, response.Address) { Success = true };
                            }
                            else
                            {
                                return new SetLeaderResponse(response.LeaderPath, response.Address)
                                {
                                    Success = false,
                                    Reason =
                                        string.Format("Failed to Acquire with status {0}", acq.Result.StatusCode)
                                };
                            }
                        });
            }
            return createSessionTask.ContinueWith(t =>
            {
                return new SetLeaderResponse(t.Result.LeaderPath, t.Result.Address)
                {
                    Success = t.Result.Success,
                    Reason = t.Result.Reason
                };
            });
        }

        private string Convert(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        private byte[] Convert(string value)
        {
            return System.Text.Encoding.UTF8.GetBytes(value);
        }

        private static string CreateKey(string seedsPath, string member)
        {
            return string.Format("{0}/{1}", seedsPath, member);
        }

        private class CreateSessionResponse
        {
            public CreateSessionResponse(string session, string leaderPath, string address, bool success, string reason)
            {
                Session = session;
                LeaderPath = leaderPath;
                Address = address;
                Success = success;
                Reason = reason;
            }

            public string Session { get; private set; }
            public string LeaderPath { get; private set; }
            public string Address { get; private set; }
            public bool Success { get; private set; }
            public string Reason { get; private set; }
        }
    }
}
