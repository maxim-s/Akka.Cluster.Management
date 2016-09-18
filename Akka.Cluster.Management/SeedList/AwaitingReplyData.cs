using System.Collections.Generic;

namespace Akka.Cluster.Management.SeedList
{
    class AwaitingReplyData : ISeedListData
    {
        public ICommand Command { get; }
        public IDictionary<string, string> AdressMapping { get; }

        public AwaitingReplyData(ICommand command, IDictionary<string, string> adressMapping)
        {
            Command = command;
            AdressMapping = adressMapping;
        }
    }
}