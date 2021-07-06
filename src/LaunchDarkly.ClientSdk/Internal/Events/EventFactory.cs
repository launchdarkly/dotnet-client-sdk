
using static LaunchDarkly.Sdk.Xamarin.Internal.Events.EventProcessorTypes;

namespace LaunchDarkly.Sdk.Xamarin.Internal.Events
{
    internal sealed class EventFactory
    {
        private readonly bool _withReasons;

        internal static readonly EventFactory Default = new EventFactory(false);
        internal static readonly EventFactory DefaultWithReasons = new EventFactory(true);

        internal EventFactory(bool withReasons)
        {
            _withReasons = withReasons;
        }

        internal EvaluationEvent NewEvaluationEvent(
            string flagKey,
            FeatureFlag flag,
            User user,
            EvaluationDetail<LdValue> result,
            LdValue defaultValue
            )
        {
            // EventFactory passes the reason parameter to this method because the server-side SDK needs to
            // look at the reason; but in this client-side SDK, we don't look at that parameter, because
            // LD has already done the relevant calculation for us and sent us the result in trackReason.
            var isExperiment = flag.trackReason;

            return new EvaluationEvent
            {
                Timestamp = UnixMillisecondTime.Now,
                User = user,
                FlagKey = flagKey,
                FlagVersion = flag.flagVersion ?? flag.version,
                Variation = result.VariationIndex,
                Value = result.Value,
                Default = defaultValue,
                Reason = (_withReasons || isExperiment) ? result.Reason : (EvaluationReason?)null,
                TrackEvents = flag.trackEvents || isExperiment,
                DebugEventsUntilDate = flag.debugEventsUntilDate
            };
        }

        internal EvaluationEvent NewDefaultValueEvaluationEvent(
            string flagKey,
            FeatureFlag flag,
            User user,
            LdValue defaultValue,
            EvaluationErrorKind errorKind
            )
        {
            return new EvaluationEvent
            {
                Timestamp = UnixMillisecondTime.Now,
                User = user,
                FlagKey = flagKey,
                FlagVersion = flag.flagVersion ?? flag.version,
                Value = defaultValue,
                Default = defaultValue,
                Reason = _withReasons ? EvaluationReason.ErrorReason(errorKind) : (EvaluationReason?)null,
                TrackEvents = flag.trackEvents,
                DebugEventsUntilDate = flag.debugEventsUntilDate
            };
        }

        internal EvaluationEvent NewUnknownFlagEvaluationEvent(
            string flagKey,
            User user,
            LdValue defaultValue,
            EvaluationErrorKind errorKind
            )
        {
            return new EvaluationEvent
            {
                Timestamp = UnixMillisecondTime.Now,
                User = user,
                FlagKey = flagKey,
                Value = defaultValue,
                Default = defaultValue,
                Reason = _withReasons ? EvaluationReason.ErrorReason(errorKind) : (EvaluationReason?)null,
            };
        }
    }
}
