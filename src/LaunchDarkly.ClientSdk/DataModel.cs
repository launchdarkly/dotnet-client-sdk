using System;
using LaunchDarkly.JsonStream;
using LaunchDarkly.Sdk.Json;

using static LaunchDarkly.Sdk.Client.Subsystems.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client
{
    /// <summary>
    /// Contains information about the internal data model for feature flag state.
    /// </summary>
    /// <remarks>
    /// The details of the data model are not public to application code (although of course developers can easily
    /// look at the code or the data) so that changes to LaunchDarkly SDK implementation details will not be breaking
    /// changes to the application. Therefore, most of the members of this class are internal. The public members
    /// provide a high-level description of model objects so that custom integration code or test code can store or
    /// serialize them.
    /// </remarks>
    public static class DataModel
    {
        /// <summary>
        /// Represents the state of a feature flag evaluation received from LaunchDarkly.
        /// </summary>
        [JsonStreamConverter(typeof(FeatureFlagJsonConverter))]
        public sealed class FeatureFlag : IEquatable<FeatureFlag>, IJsonSerializable
        {
            internal LdValue Value { get; }
            internal int? Variation { get; }
            internal EvaluationReason? Reason { get; }
            internal int Version { get; }
            internal int? FlagVersion { get; }
            internal bool TrackEvents { get; }
            internal bool TrackReason { get; }
            internal UnixMillisecondTime? DebugEventsUntilDate { get; }

            internal FeatureFlag(
                LdValue value,
                int? variation,
                EvaluationReason? reason,
                int version,
                int? flagVersion,
                bool trackEvents,
                bool trackReason,
                UnixMillisecondTime? debugEventsUntilDate
                )
            {
                Value = value;
                Variation = variation;
                Reason = reason;
                Version = version;
                FlagVersion = flagVersion;
                TrackEvents = trackEvents;
                TrackReason = trackReason;
                DebugEventsUntilDate = debugEventsUntilDate;
            }

            /// <inheritdoc/>
            public override bool Equals(object obj) =>
                Equals(obj as FeatureFlag);

            /// <inheritdoc/>
            public bool Equals(FeatureFlag otherFlag) =>
                Value.Equals(otherFlag.Value)
                && Variation == otherFlag.Variation
                && Reason.Equals(otherFlag.Reason)
                && Version == otherFlag.Version
                && FlagVersion == otherFlag.FlagVersion
                && TrackEvents == otherFlag.TrackEvents
                && DebugEventsUntilDate == otherFlag.DebugEventsUntilDate;

            /// <inheritdoc/>
            public override int GetHashCode() =>
                Value.GetHashCode();

            /// <inheritdoc/>
            public override string ToString() =>
                string.Format("({0},{1},{2},{3},{4},{5},{6},{7})",
                    Value, Variation, Reason, Version, FlagVersion, TrackEvents, TrackReason, DebugEventsUntilDate);

            internal ItemDescriptor ToItemDescriptor() =>
                new ItemDescriptor(Version, this);
        }

        internal sealed class FeatureFlagJsonConverter : IJsonStreamConverter
        {
            public object ReadJson(ref JReader reader)
            {
                return ReadJsonValue(ref reader);
            }

            public static FeatureFlag ReadJsonValue(ref JReader reader)
            {
                LdValue value = LdValue.Null;
                int version = 0;
                int? flagVersion = null;
                int? variation = null;
                EvaluationReason? reason = null;
                bool trackEvents = false;
                bool trackReason = false;
                UnixMillisecondTime? debugEventsUntilDate = null;

                for (var or = reader.Object(); or.Next(ref reader);)
                {
                    // The use of multiple == tests instead of switch allows for a slight optimization on
                    // some platforms where it wouldn't always need to allocate a string for or.Name. See:
                    // https://github.com/launchdarkly/dotnet-jsonstream/blob/main/src/LaunchDarkly.JsonStream/PropertyNameToken.cs
                    var name = or.Name;
                    if (name == "value")
                    {
                        value = LdJsonConverters.LdValueConverter.ReadJsonValue(ref reader);
                    }
                    else if (name == "version")
                    {
                        version = reader.Int();
                    }
                    else if (name == "flagVersion")
                    {
                        flagVersion = reader.IntOrNull();
                    }
                    else if (name == "variation")
                    {
                        variation = reader.IntOrNull();
                    }
                    else if (name == "reason")
                    {
                        reason = LdJsonConverters.EvaluationReasonConverter.ReadJsonNullableValue(ref reader);
                    }
                    else if (name == "trackEvents")
                    {
                        trackEvents = reader.Bool();
                    }
                    else if (name == "trackReason")
                    {
                        trackReason = reader.Bool();
                    }
                    else if (name == "debugEventsUntilDate")
                    {
                        var dt = reader.LongOrNull();
                        if (dt.HasValue)
                        {
                            debugEventsUntilDate = UnixMillisecondTime.OfMillis(dt.Value);
                        }
                    }
                }

                return new FeatureFlag(
                    value,
                    variation,
                    reason,
                    version,
                    flagVersion,
                    trackEvents,
                    trackReason,
                    debugEventsUntilDate
                    );
            }

            public void WriteJson(object o, IValueWriter writer)
            {
                if (!(o is FeatureFlag value))
                {
                    throw new InvalidOperationException();
                }
                WriteJsonValue(value, writer);
            }

            public static void WriteJsonValue(FeatureFlag value, IValueWriter writer)
            {
                using (var ow = writer.Object())
                {
                    LdJsonConverters.LdValueConverter.WriteJsonValue(value.Value, ow.Name("value"));
                    ow.Name("version").Int(value.Version);
                    ow.MaybeName("flagVersion", value.FlagVersion.HasValue).Int(value.FlagVersion.GetValueOrDefault());
                    ow.MaybeName("variation", value.Variation.HasValue).Int(value.Variation.GetValueOrDefault());
                    if (value.Reason.HasValue)
                    {
                        LdJsonConverters.EvaluationReasonConverter.WriteJsonValue(value.Reason.Value, ow.Name("reason"));
                    }
                    ow.MaybeName("trackEvents", value.TrackEvents).Bool(value.TrackEvents);
                    ow.MaybeName("trackReason", value.TrackReason).Bool(value.TrackReason);
                    ow.MaybeName("debugEventsUntilDate", value.DebugEventsUntilDate.HasValue)
                        .Long(value.DebugEventsUntilDate.GetValueOrDefault().Value);
                }
            }
        }
    }
}
