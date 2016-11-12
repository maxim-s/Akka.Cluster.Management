using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Cluster.Management.ServiceDiscovery;
using Xunit;

namespace Akka.Cluster.Management.Consul.Tests
{
    public class ConsulServiceDiscoveryClientSpec
    {
        private readonly IServiceDiscoveryClient _client = new ConsulServiceDiscoveryClient();
        private const string SeedsPath = "SeedsPath";
        [Fact]
        public async void Should_register_seed()
        {
            var resp1 =  await _client.Create(SeedsPath, "member1");
            var resp2 =  await _client.Create(SeedsPath, "member2");
            Assert.True(resp1.Success);
            Assert.True(resp2.Success);
        }

        [Fact]
        public async void Should_return_registered_seeds()
        {
            await _client.Create(SeedsPath, "member1");
            await _client.Create(SeedsPath, "member2");
            var resp = await _client.Get(SeedsPath);
            Assert.True(resp.Success);
            Assert.True(resp.Nodes.Count == 2);
            Assert.True(resp.Nodes.Count == 2);
            var enumerable = resp.Nodes.Select(kv=> kv.Value).ToList();
            Assert.True(enumerable.Contains("member1") && enumerable.Contains("member2"));
        }

        [Fact]
        public async void Should_not_return_registered_nodes()
        {
            var resp = await _client.Get("notexist");
            Assert.False(resp.Success);
            Assert.Null(resp.Nodes);
        }

        [Fact]
        public async void Should_delete_registered_nodes()
        {
            await _client.Create(SeedsPath, "member1");
            await _client.Create(SeedsPath, "member2");
            var resp = await _client.Delete(SeedsPath, null, true);
            Assert.True(resp.Success);
            var get = await _client.Get(SeedsPath);
            Assert.Null(get.Nodes);
        }

        [Fact]
        public async void Should_delete_registered_member()
        {
            await _client.Create(SeedsPath, "member1");
            await _client.Create(SeedsPath, "member2");
            var resp = await _client.Delete(SeedsPath, "member1");
            Assert.True(resp.Success);
            var get = await _client.Get(SeedsPath);
            Assert.True(get.Nodes.Count == 1);
        }

        [Fact]
        public async void Should_set_leader()
        {
            var resp = await _client.SetLeader("leader/path", "address", TimeSpan.FromSeconds(10));
            Assert.True(resp.Success);
            Assert.Equal("address", resp.Address);
            Assert.Equal("leader/path", resp.LeaderPath);
        }
    }
}
