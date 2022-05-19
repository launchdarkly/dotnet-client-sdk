using System.Threading.Tasks;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal.DataStores;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal static class ComponentsImpl
    {
        internal sealed class NullDataSourceFactory : IDataSourceFactory
        {
            public IDataSource CreateDataSource(
                LdClientContext context,
                IDataSourceUpdateSink updateSink,
                User currentUser,
                bool inBackground
                ) =>
                new NullDataSource();
        }

        internal sealed class NullDataSource : IDataSource
        {
            public bool Initialized => true;

            public void Dispose() { }

            public Task<bool> Start() => Task.FromResult(true);
        }

        internal sealed class NullEventProcessorFactory : IEventProcessorFactory
        {
            internal static readonly NullEventProcessorFactory Instance = new NullEventProcessorFactory();

            public IEventProcessor CreateEventProcessor(LdClientContext context) =>
                NullEventProcessor.Instance;
        }

        internal sealed class NullEventProcessor : IEventProcessor
        {
            internal static readonly NullEventProcessor Instance = new NullEventProcessor();

            public void Dispose() { }

            public void Flush() { }

            public void RecordCustomEvent(EventProcessorTypes.CustomEvent e) { }

            public void RecordEvaluationEvent(EventProcessorTypes.EvaluationEvent e) { }

            public void RecordIdentifyEvent(EventProcessorTypes.IdentifyEvent e) { }

            public void SetOffline(bool offline) { }
        }

    }
}
