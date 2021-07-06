using System;
using LaunchDarkly.JsonStream;
using LaunchDarkly.Sdk.Json;

namespace LaunchDarkly.Sdk.Client.Internal
{
    [JsonStreamConverter(typeof(FeatureFlag.JsonConverter))]
    internal sealed class FeatureFlag : IEquatable<FeatureFlag>, IJsonSerializable
    {
        public readonly LdValue value;
        public readonly int version;
        public readonly int? flagVersion;
        public readonly bool trackEvents;
        public readonly bool trackReason;
        public readonly int? variation;
        public readonly UnixMillisecondTime? debugEventsUntilDate;
        public readonly EvaluationReason? reason;

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

        internal sealed class JsonConverter : IJsonStreamConverter
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

                for (var or = reader.Object(); or.Next(ref reader); )
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

                return new FeatureFlag(value, version, flagVersion, trackEvents, trackReason, variation,
                    debugEventsUntilDate, reason);
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
