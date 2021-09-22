using System;
using LaunchDarkly.JsonStream;
using LaunchDarkly.Sdk.Json;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

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
            internal LdValue value { get; }
            internal int? variation { get; }
            internal EvaluationReason? reason { get; }
            internal int version { get; }
            internal int? flagVersion { get; }
            internal bool trackEvents { get; }
            internal bool trackReason { get; }
            internal UnixMillisecondTime? debugEventsUntilDate { get; }

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
                this.value = value;
                this.variation = variation;
                this.reason = reason;
                this.version = version;
                this.flagVersion = flagVersion;
                this.trackEvents = trackEvents;
                this.trackReason = trackReason;
                this.debugEventsUntilDate = debugEventsUntilDate;
            }

            /// <inheritdoc/>
            public override bool Equals(object obj) =>
                Equals(obj as FeatureFlag);

            /// <inheritdoc/>
            public bool Equals(FeatureFlag otherFlag) =>
                value.Equals(otherFlag.value)
                && variation == otherFlag.variation
                && reason.Equals(otherFlag.reason)
                && version == otherFlag.version
                && flagVersion == otherFlag.flagVersion
                && trackEvents == otherFlag.trackEvents
                && debugEventsUntilDate == otherFlag.debugEventsUntilDate;

            /// <inheritdoc/>
            public override int GetHashCode() =>
                value.GetHashCode();

            /// <inheritdoc/>
            public override string ToString() =>
                string.Format("({0},{1},{2},{3},{4},{5},{6},{7})",
                    value, variation, reason, version, flagVersion, trackEvents, trackReason, debugEventsUntilDate);

            internal ItemDescriptor ToItemDescriptor() =>
                new ItemDescriptor(version, this);
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
                    // https://github.com/launchdarkly/dotnet-jsonstream/blob/master/src/LaunchDarkly.JsonStream/PropertyNameToken.cs
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
                    LdJsonConverters.LdValueConverter.WriteJsonValue(value.value, ow.Name("value"));
                    ow.Name("version").Int(value.version);
                    ow.MaybeName("flagVersion", value.flagVersion.HasValue).Int(value.flagVersion.GetValueOrDefault());
                    ow.MaybeName("variation", value.variation.HasValue).Int(value.variation.GetValueOrDefault());
                    if (value.reason.HasValue)
                    {
                        LdJsonConverters.EvaluationReasonConverter.WriteJsonValue(value.reason.Value, ow.Name("reason"));
                    }
                    ow.MaybeName("trackEvents", value.trackEvents).Bool(value.trackEvents);
                    ow.MaybeName("trackReason", value.trackReason).Bool(value.trackReason);
                    ow.MaybeName("debugEventsUntilDate", value.debugEventsUntilDate.HasValue)
                        .Long(value.debugEventsUntilDate.GetValueOrDefault().Value);
                }
            }
        }
    }
}
