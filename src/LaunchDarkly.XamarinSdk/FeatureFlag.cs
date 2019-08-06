using System;
using Newtonsoft.Json.Linq;
using LaunchDarkly.Client;
using LaunchDarkly.Common;

namespace LaunchDarkly.Xamarin
{
    internal sealed class FeatureFlag : IEquatable<FeatureFlag>
    {
        public JToken value;
        public int version;
        public int? flagVersion;
        public bool trackEvents;
        public bool trackReason;
        public int? variation;
        public long? debugEventsUntilDate;
        public EvaluationReason reason;

        public bool Equals(FeatureFlag otherFlag)
        {
            return JToken.DeepEquals(value, otherFlag.value)
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

        public static FeatureFlagEvent Default(string key)
        {
            return new FeatureFlagEvent(key, new FeatureFlag());
        }
        
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
