using System;
using Akka.Actor;
using Akka.Cluster.Management.ServiceDiscovery;

namespace Akka.Cluster.Management.LeaderEntry
{
    public class LeaderEntryActor : FSM<LeaderEntryState, LeaderEntryData>
    {
        private readonly string _address;
        private readonly IServiceDiscoveryClient _client;
        private readonly ClusterDiscoverySettings _settings;

        public LeaderEntryActor(string address, IServiceDiscoveryClient client, ClusterDiscoverySettings settings)
        {
            _address = address;
            _client = client;
            _settings = settings;

            var refreshInterval = TimeSpan.FromMilliseconds(_settings.LeaderEntryTTL.TotalMilliseconds / 2);

            When(LeaderEntryState.Idle, @event =>
            {
                if (@event.FsmEvent is StateTimeout)
                {
                    if (@event.StateData.AssumeEntryExists)
                    {
                        RefreshLeaderEntry();
                    }
                    else
                    {
                        CreateLeaderEntry();
                    }
                }
                return GoTo(LeaderEntryState.AwaitingReply);
            });

            When(LeaderEntryState.AwaitingReply, @event =>
            {
                if (@event.FsmEvent is ResponseEvent)
                {
                    return GoTo(LeaderEntryState.Idle).Using(new LeaderEntryData (true)).ForMax(refreshInterval);
                }

                var errorEvent = @event.FsmEvent as ErrorEvent;
                if (errorEvent != null)
                {
                    if (errorEvent.Status == ErrorStatus.KeyNotFound || errorEvent.Status == ErrorStatus.TestFailed)
                    {
                        return GoTo(LeaderEntryState.Idle)
                            .Using(new LeaderEntryData(false))
                            .ForMax(refreshInterval);
                    }
                    
                    // TODO: log
                    return GoTo(LeaderEntryState.Idle)
                        .Using(new LeaderEntryData(@event.StateData.AssumeEntryExists))
                        .ForMax(_settings.RetryDelay);
                }

                if (@event.FsmEvent is Status.Failure)
                {
                    return GoTo(LeaderEntryState.Idle)
                        .Using(new LeaderEntryData(@event.StateData.AssumeEntryExists))
                        .ForMax(_settings.RetryDelay);
                }
                return null;
            });

            StartWith(LeaderEntryState.Idle, new LeaderEntryData(true), refreshInterval);
            Initialize();
        }


        /// <summary>
        /// Create the leader entry, assuming it does not exist.
        /// 
        /// This method is used when the leader entry has expired while the leader node was unable to reach etcd, or when
        /// the leader entry was hijacked by another node.System operator will eventually shut down one of the contending
        ///  leaders, and if the current node prevails it will reclaim the leader entry after it expires.
        /// </summary>
        private void CreateLeaderEntry()
        {
            _client.CompareAndSet(_settings.LeaderPath, _address, _settings.LeaderEntryTTL);
        }

        /// <summary>
        /// Refresh the entry at leader path, assuming that it exists and the current value is our node's address.
        /// 
        /// This method is used during the normal refresh cycle.
        /// </summary>
        private void RefreshLeaderEntry()
        {
            _client.CompareAndSet(_settings.LeaderPath, _address, _settings.LeaderEntryTTL);
        }
    }
}
