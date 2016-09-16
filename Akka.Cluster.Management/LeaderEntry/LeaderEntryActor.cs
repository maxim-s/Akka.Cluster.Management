using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Cluster.Management.LeaderEntry
{
    public class LeaderEntryActor : FSM<LeaderEntryState, ILeaderEntryData>
    {
        private readonly string _address;
        private readonly IServiceDiscoveryClient _client;
        private readonly ClusterDiscoverySettings _settings;

        public LeaderEntryActor(string address, IServiceDiscoveryClient client, ClusterDiscoverySettings settings)
        {
            _address = address;
            _client = client;
            _settings = settings;
        }
    }
}
