using System;
using LaunchDarkly.Client;
using LaunchDarkly.Common;
using Newtonsoft.Json;

namespace LaunchDarkly.Xamarin
{
    internal sealed class FeatureFlag : IEquatable<FeatureFlag>
    {
        public readonly LdValue value;
        public readonly int version;
        public readonly int? flagVersion;
        public readonly bool trackEvents;
        public readonly bool trackReason;
        public readonly int? variation;
        public readonly long? debugEventsUntilDate;
        public readonly EvaluationReason reason;

        [JsonConstructor]
        public FeatureFlag(LdValue value, int version, int? flagVersion, bool trackEvents, bool trackReason,
            int? variation, long? debugEventsUntilDate, EvaluationReason reason)
        {
            this.value = value;
            this.version = version;
            this.flagVersion = flagVersion;
            this.trackEvents = trackEvents;
            this.trackReason = trackReason;
            this.variation = variation;
            this.debugEventsUntilDate = debugEventsUntilDate;
            this.reason = reason;
        }

        public bool Equals(FeatureFlag otherFlag)
        {
            return value.Equals(otherFlag.value)
                        && version == otherFlag.version
                        && flagVersion == otherFlag.flagVersion
                        && trackEvents == otherFlag.trackEvents
                        && variation == otherFlag.variation
                        && debugEventsUntilDate == otherFlag.debugEventsUntilDate
                        && reason == otherFlag.reason;
        }
    }

    /// <summary>
    /// The IFlagEventProperties abstraction is used by LaunchDarkly.Common to communicate properties
    /// that affect event generation. We can't just have FeatureFlag itself implement that interface,
    /// because it doesn't actually contain its own flag key.
    /// </summary>
    internal struct FeatureFlagEvent : IFlagEventProperties
    {
        private readonly FeatureFlag _featureFlag;
        private readonly string _key;

        public FeatureFlagEvent(string key, FeatureFlag featureFlag)
        {
            _featureFlag = featureFlag;
            _key = key;
        }

        public string Key => _key;
        public int EventVersion => _featureFlag.flagVersion ?? _featureFlag.version;
        public bool TrackEvents => _featureFlag.trackEvents;
        public long? DebugEventsUntilDate => _featureFlag.debugEventsUntilDate;

        public bool IsExperiment(EvaluationReason reason)
        {
            // EventFactory passes the reason parameter to this method because the server-side SDK needs to
            // look at the reason; but in this client-side SDK, we don't look at that parameter, because
            // LD has already done the relevant calculation for us and sent us the result in trackReason.
            return _featureFlag.trackReason;
        }
    }
}
