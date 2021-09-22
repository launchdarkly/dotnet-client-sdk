using System.Threading.Tasks;
using LaunchDarkly.Sdk.Client.Interfaces;

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
            internal static NullEventProcessorFactory Instance = new NullEventProcessorFactory();

            public IEventProcessor CreateEventProcessor(LdClientContext context) =>
                NullEventProcessor.Instance;
        }

        internal sealed class NullEventProcessor : IEventProcessor
        {
            internal static NullEventProcessor Instance = new NullEventProcessor();

            public void Dispose() { }

            public void Flush() { }

            public void RecordAliasEvent(EventProcessorTypes.AliasEvent e) { }

            public void RecordCustomEvent(EventProcessorTypes.CustomEvent e) { }

            public void RecordEvaluationEvent(EventProcessorTypes.EvaluationEvent e) { }

            public void RecordIdentifyEvent(EventProcessorTypes.IdentifyEvent e) { }

            public void SetOffline(bool offline) { }
        }

        internal sealed class NullPersistentDataStoreFactory : IPersistentDataStoreFactory
        {
            internal static NullPersistentDataStoreFactory Instance = new NullPersistentDataStoreFactory();

            public IPersistentDataStore CreatePersistentDataStore(LdClientContext context) =>
                NullPersistentDataStore.Instance;
        }

        internal sealed class NullPersistentDataStore : IPersistentDataStore
        {
            internal static NullPersistentDataStore Instance = new NullPersistentDataStore();

            public string GetAll(User user) => null;

            public void Init(User user, string allData) { }

            public void Dispose() { }
        }
    }
}
