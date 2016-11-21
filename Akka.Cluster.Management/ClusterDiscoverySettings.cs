using System;
using Akka.Configuration;

namespace Akka.Cluster.Management
{
    public class ClusterDiscoverySettings
    {
        public ClusterDiscoverySettings(string leaderEntryPath, string seedsPath, TimeSpan leaderEntryTtl,
            TimeSpan retryDelay, TimeSpan seedsJoinTimeout, TimeSpan seedsFetchTimeout, string host, int port,
            string path)
        {
            LeaderPath = leaderEntryPath;
            SeedsPath = seedsPath;
            LeaderEntryTTL = leaderEntryTtl;
            RetryDelay = retryDelay;
            SeedsJoinTimeout = seedsJoinTimeout;
            SeedsFetchTimeout = seedsFetchTimeout;
            Host = host;
            Port = port;
            BasePath = path;
        }

        public int Port { get; private set; }

        public string Host { get; private set; }

        /// <summary>
        ///     Path, relative to seeds, where seed nodes information is stored
        /// </summary>
        public string SeedsPath { get; private set; }

        public TimeSpan LeaderEntryTTL { get; private set; }

        public TimeSpan RetryDelay { get; private set; }

        public string LeaderPath { get; private set; }

        public TimeSpan SeedsJoinTimeout { get; private set; }
        public TimeSpan SeedsFetchTimeout { get; private set; }
        public string BasePath { get; private set; }

        public static ClusterDiscoverySettings Load(Config config)
        {
            var c = config.GetConfig("akka.cluster.discovery");
            return new ClusterDiscoverySettings(
                c.GetString("leaderPath"),
                c.GetString("seedsPath"),
                GetDuration(c, "leaderEntry"),
                GetDuration(c, "retry"),
                GetDuration(c, "seedsJoin"),
                GetDuration(c, "seedsFetch"),
                c.GetString("host"),
                c.GetInt("port"),
                c.GetString("path"));
        }

        private static TimeSpan GetDuration(Config config, string path)
        {
            var t = config.GetConfig("timeouts");
            return t.GetTimeSpan(path);
        }
    }
}