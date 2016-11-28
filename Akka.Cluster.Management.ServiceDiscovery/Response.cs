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
            Success = true;
        }

        public IImmutableDictionary<string,string> Nodes { get; private set; }
    }

    public class CreateNodeResponse : Response
    {
        public CreateNodeResponse(string address)
        {
            Address = address;
            Success = true;
        }

        public string Address { get; private set; }
    }

    public class DeleteNodeResponse : Response
    {
        public DeleteNodeResponse(string address)
        {
            Address = address;
            Success = true;
        }

        public string Address { get; private set; }
    }

    public class SetLeaderResponse : Response
    {
        public SetLeaderResponse(string leaderPath, string address)
        {
            LeaderPath = leaderPath;
            Address = address;
            Success = true;
        }

        public string Address { get; private set; }
        public string LeaderPath { get; private set; }
    }

    public class StartResponse : Response
    {
        public StartResponse(string address)
        {
            Address = address;
            Success = true;
        }

        public string Address { get; private set; }
    }

    public class NodeExistResponse : Response
    {
        public NodeExistResponse(string path, string address)
        {
            Path = path;
            Address = address;
            Success = true;
        }

        public string Path { get; private set; }
        public string Address { get; private set; }
    }
}