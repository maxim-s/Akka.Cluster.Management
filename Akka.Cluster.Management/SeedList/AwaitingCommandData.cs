using System.Collections;
using System.Collections.Generic;

namespace Akka.Cluster.Management.SeedList
{
    class AwaitingCommandData : ISeedListData
    {
        public IDictionary<string, string> AddressMapping { get; set; }
    }
}