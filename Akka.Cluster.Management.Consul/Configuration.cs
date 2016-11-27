using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Cluster.Management.Consul
{
    public class Configuration
    {
        /// <summary>
        /// The Uri to connect to the Consul agent.
        /// </summary>
        public Uri Address { get; set; }

        /// <summary>
        /// Datacenter to provide with each request. If not provided, the default agent datacenter is used.
        /// </summary>
        public string Datacenter { get; set; }

        /// <summary>
        /// Credentials to use for access to the HTTP API.
        /// This is only needed if an authenticating service exists in front of Consul; Token is used for ACL authentication by Consul.
        /// </summary>
        public NetworkCredential HttpAuth { get; set; }

        /// <summary>
        /// Token is used to provide an ACL token which overrides the agent's default token. This ACL token is used for every request by
        /// clients created using this configuration.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// WaitTime limits how long a Watch will block. If not provided, the agent default values will be used.
        /// </summary>
        public TimeSpan? WaitTime { get; set; }

    }
}
