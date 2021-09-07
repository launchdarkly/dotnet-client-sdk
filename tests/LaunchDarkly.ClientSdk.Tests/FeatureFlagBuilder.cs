using LaunchDarkly.Sdk.Client.Internal;

using static LaunchDarkly.Sdk.Client.DataModel;

namespace LaunchDarkly.Sdk.Client
{
    internal class FeatureFlagBuilder
    {
        private LdValue _value = LdValue.Null;
        private int _version;
        private int? _variation;
        private int? _flagVersion;
#pragma warning disable 0649
        // Currently trackEvents, trackReason, and debugEventsUntilDate are never set in the tests. That's because those properties
        // are only used by DefaultEventProcessor (in LaunchDarkly.CommonSdk), which has its own tests against an abstraction of the
        // same properties.
        private bool _trackEvents;
        private bool _trackReason;
        private UnixMillisecondTime? _debugEventsUntilDate;
#pragma warning disable 0649
        private EvaluationReason? _reason;

        public FeatureFlagBuilder()
        {
        }

        public FeatureFlag Build()
        {
            return new FeatureFlag(_value, _variation, _reason, _version, _flagVersion, _trackEvents, _trackReason, _debugEventsUntilDate);
        }

        public FeatureFlagBuilder Value(LdValue value)
        {
            _value = value;
            return this;
        }

        public FeatureFlagBuilder FlagVersion(int? flagVersion)
        {
            _flagVersion = flagVersion;
            return this;
        }

        public FeatureFlagBuilder Version(int version)
        {
            _version = version;
            return this;
        }

        public FeatureFlagBuilder Variation(int? variation)
        {
            _variation = variation;
            return this;
        }

        public FeatureFlagBuilder Reason(EvaluationReason? reason)
        {
            _reason = reason;
            return this;
        }
    }
}
