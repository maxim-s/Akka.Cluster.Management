using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Akka.Cluster.Management.ServiceDiscovery
{
    public class Response
    {
        public string Key { get;  set; }

        public bool Success { get; set; }

        public string Reason { get; set; }

    }

    public class GetNodesResponse : Response
    {
        public GetNodesResponse(IDictionary<string,string> nodes)
        {
            Nodes = nodes != null? nodes.ToImmutableDictionary(): null;
        }

        public IImmutableDictionary<string,string> Nodes { get; private set; }
    }

    public class CreateNodeResponse : Response
    {
        public CreateNodeResponse(string address)
        {
            Address = address;
        }

        public string Address { get; private set; }
    }

    public class DeleteNodeResponse : Response
    {
        public DeleteNodeResponse(string address)
        {
            Address = address;
        }

        public string Address { get; private set; }
    }

    public class SetLeaderResponse : Response
    {
        public SetLeaderResponse(string leaderPath, string address )
        {
            LeaderPath = leaderPath;
            Address = address;
        }

        public string Address { get; private set; }
        public string LeaderPath { get; private set; }
    }
}