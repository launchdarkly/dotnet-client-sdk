using LaunchDarkly.Sdk.Internal.Events;

namespace LaunchDarkly.Sdk.Client.Internal.Events
{
    internal sealed class DefaultEventProcessorWrapper : IEventProcessor
    {
        private EventProcessor _eventProcessor;

        internal DefaultEventProcessorWrapper(EventProcessor eventProcessor)
        {
            _eventProcessor = eventProcessor;
        }

        public void RecordEvaluationEvent(EventProcessorTypes.EvaluationEvent e)
        {
            _eventProcessor.RecordEvaluationEvent(new EventTypes.EvaluationEvent
            {
                Timestamp = e.Timestamp,
                User = e.User,
                FlagKey = e.FlagKey,
                FlagVersion = e.FlagVersion,
                Variation = e.Variation,
                Value = e.Value,
                Default = e.Default,
                Reason = e.Reason,
                TrackEvents = e.TrackEvents,
                DebugEventsUntilDate = e.DebugEventsUntilDate
            });
        }

        public void RecordIdentifyEvent(EventProcessorTypes.IdentifyEvent e)
        {
            _eventProcessor.RecordIdentifyEvent(new EventTypes.IdentifyEvent
            {
                Timestamp = e.Timestamp,
                User = e.User
            });
        }

        public void RecordCustomEvent(EventProcessorTypes.CustomEvent e)
        {
            _eventProcessor.RecordCustomEvent(new EventTypes.CustomEvent
            {
                Timestamp = e.Timestamp,
                User = e.User,
                EventKey = e.EventKey,
                Data = e.Data,
                MetricValue = e.MetricValue
            });
        }

        public void SetOffline(bool offline) =>
            _eventProcessor.SetOffline(offline);

        public void Flush() => _eventProcessor.Flush();

        public void Dispose() => _eventProcessor.Dispose();
    }
}
