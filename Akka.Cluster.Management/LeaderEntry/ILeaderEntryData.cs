using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Cluster.Management.LeaderEntry
{
    public class LeaderEntryData
    {
        public LeaderEntryData()
        {
            
        }

        public LeaderEntryData(bool assumeEntryExists)
        {
            AssumeEntryExists = assumeEntryExists;
        }

        public bool AssumeEntryExists { get; set; }
    }
}
