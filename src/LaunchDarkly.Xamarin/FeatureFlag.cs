using System;
using Newtonsoft.Json.Linq;
using LaunchDarkly.Common;

namespace LaunchDarkly.Xamarin
{
    public class FeatureFlag : IEquatable<FeatureFlag>
    {
        public JToken value;
        public int version;
        public bool trackEvents;
        public int? variation;
        public long? debugEventsUntilDate;

        public bool Equals(FeatureFlag otherFlag)
        {
            return JToken.DeepEquals(value, otherFlag.value)
                        && version == otherFlag.version
                        && trackEvents == otherFlag.trackEvents
                        && variation == otherFlag.variation
                        && debugEventsUntilDate == otherFlag.debugEventsUntilDate;
        }
    }

    internal class FeatureFlagEvent : IFlagEventProperties
    {
        private FeatureFlag _featureFlag;
        private string _key;

        public static FeatureFlagEvent Default(string key)
        {
            return new FeatureFlagEvent(key, new FeatureFlag());
        }

        private FeatureFlagEvent() { }

        public FeatureFlagEvent(string key, FeatureFlag featureFlag)
        {
            _featureFlag = featureFlag;
            _key = key;
        }

        public string Key => _key;
        public int Version => _featureFlag.version;
        public bool TrackEvents => _featureFlag.trackEvents;
        public long? DebugEventsUntilDate => _featureFlag.debugEventsUntilDate;
    }
}
