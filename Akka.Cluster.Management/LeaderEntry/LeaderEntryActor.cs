using System;
using Akka.Actor;

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

            var refreshInterval = settings.LeaderEntryTTL / 2;

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
                GoTo(LeaderEntryState.AwaitingReply);
                return null;
            });

            When(LeaderEntryState.AwaitingReply, @event =>
            {
                if (@event.FsmEvent is ResponseEvent)
                {
                    GoTo(LeaderEntryState.Idle).Using(new LeaderEntryData (true)).ForMax(new TimeSpan(refreshInterval));
                }

                var errorEvent = @event.FsmEvent as ErrorEvent;
                if (errorEvent != null)
                {
                    if (errorEvent.Status == ErrorStatus.KeyNotFound || errorEvent.Status == ErrorStatus.TestFailed)
                    {
                        GoTo(LeaderEntryState.Idle)
                            .Using(new LeaderEntryData(false))
                            .ForMax(new TimeSpan(refreshInterval));
                    }
                    else
                    {
                        // TODO: log
                        GoTo(LeaderEntryState.Idle)
                            .Using(new LeaderEntryData(@event.StateData.AssumeEntryExists))
                            .ForMax(new TimeSpan(_settings.RetryDelay));
                    }
                }

                if (@event.FsmEvent is Status.Failure)
                {
                    GoTo(LeaderEntryState.Idle)
                        .Using(new LeaderEntryData(@event.StateData.AssumeEntryExists))
                        .ForMax(new TimeSpan(_settings.RetryDelay));
                }
                return null;
            });

            StartWith(LeaderEntryState.Idle, new LeaderEntryData(true), new TimeSpan(refreshInterval));
            Initialize();
        }


        private void CreateLeaderEntry()
        {
            throw new NotImplementedException();
        }

        private void RefreshLeaderEntry()
        {
            throw new NotImplementedException();
        }
    }
}
