using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Cluster.Management.LeaderEntry
{
    public class ErrorEvent
    {
        public ErrorStatus Status { get; set; }
    }

    public enum ErrorStatus
    {
        TestFailed,
        KeyNotFound
    }
}
