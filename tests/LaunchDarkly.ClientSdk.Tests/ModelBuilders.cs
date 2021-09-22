using System.Collections.Generic;

using static LaunchDarkly.Sdk.Client.DataModel;
using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client
{
    internal class FeatureFlagBuilder
    {
        private LdValue _value = LdValue.Null;
        private int _version;
        private int? _variation;
        private int? _flagVersion;
        private bool _trackEvents;
        private bool _trackReason;
        private UnixMillisecondTime? _debugEventsUntilDate;
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

        public FeatureFlagBuilder Value(bool value) => Value(LdValue.Of(value));

        public FeatureFlagBuilder Value(string value) => Value(LdValue.Of(value));

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

        public FeatureFlagBuilder TrackEvents(bool trackEvents)
        {
            _trackEvents = trackEvents;
            return this;
        }

        public FeatureFlagBuilder TrackReason(bool trackReason)
        {
            _trackReason = trackReason;
            return this;
        }

        public FeatureFlagBuilder DebugEventsUntilDate(UnixMillisecondTime? debugEventsUntilDate)
        {
            _debugEventsUntilDate = debugEventsUntilDate;
            return this;
        }
    }

    internal class DataSetBuilder
    {
        private List<KeyValuePair<string, ItemDescriptor>> _items = new List<KeyValuePair<string, ItemDescriptor>>();

        public DataSetBuilder Add(string key, FeatureFlag flag)
        {
            _items.Add(new KeyValuePair<string, ItemDescriptor>(key, flag.ToItemDescriptor()));
            return this;
        }

        public DataSetBuilder Add(string key, int version, LdValue value, int variation) =>
            Add(key, new FeatureFlagBuilder().Version(version).Value(value).Variation(variation).Build());

        public DataSetBuilder AddDeleted(string key, int version)
        {
            _items.Add(new KeyValuePair<string, ItemDescriptor>(key, new ItemDescriptor(version, null)));
            return this;
        }

        public FullDataSet Build() => new FullDataSet(_items);
    }
}
