using System;

namespace LaunchDarkly.Sdk.Client.Internal.Events
{
    internal interface IEventProcessor : IDisposable
    {
        void RecordEvaluationEvent(EventProcessorTypes.EvaluationEvent e);

        void RecordIdentifyEvent(EventProcessorTypes.IdentifyEvent e);

        void RecordCustomEvent(EventProcessorTypes.CustomEvent e);

        void SetOffline(bool offline);

        void Flush();
    }
}
