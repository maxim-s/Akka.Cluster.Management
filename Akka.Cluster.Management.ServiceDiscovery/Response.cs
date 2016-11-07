using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Akka.Cluster.Management.ServiceDiscovery
{
    public class Response
    {
        public string Key { get;  set; }

        public bool Success { get; set; } = true;

        public string Reason { get; set; }

    }

    public class GetNodesResponse : Response
    {
        public GetNodesResponse(IDictionary<string,string> nodes)
        {
                Nodes = nodes.ToImmutableDictionary();
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

    public class CreateNodeFailResponse : Response
    {
        public CreateNodeFailResponse(string response)
        {
            Response = response;
        }

        public string Response { get; private set; }
    }

    public class DeleteNodeResponse : Response
    {
        public DeleteNodeResponse(string address)
        {
            Address = address;
        }

        public string Address { get; private set; }
    }
}