using System;
using Newtonsoft.Json;

namespace LaunchDarkly.Sdk.Xamarin
{
    internal sealed class FeatureFlag : IEquatable<FeatureFlag>
    {
        public readonly LdValue value;
        public readonly int version;
        public readonly int? flagVersion;
        public readonly bool trackEvents;
        public readonly bool trackReason;
        public readonly int? variation;
        public readonly UnixMillisecondTime? debugEventsUntilDate;
        public readonly EvaluationReason? reason;

        [JsonConstructor]
        public FeatureFlag(LdValue value, int version, int? flagVersion, bool trackEvents, bool trackReason,
            int? variation, UnixMillisecondTime? debugEventsUntilDate, EvaluationReason? reason)
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
                        && reason.Equals(otherFlag.reason);
        }
    }
}
