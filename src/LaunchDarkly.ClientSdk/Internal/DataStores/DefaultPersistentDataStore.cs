using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    internal sealed class DefaultPersistentDataStore : IPersistentDataStore
    {
        private readonly Logger _log;

        internal DefaultPersistentDataStore(Logger log)
        {
            _log = log;
        }

        public void Init(User user, string allData) =>
            PlatformSpecific.Preferences.Set(Constants.FLAGS_KEY_PREFIX + user.Key, allData, _log);

        string IPersistentDataStore.GetAll(User user) =>
            PlatformSpecific.Preferences.Get(Constants.FLAGS_KEY_PREFIX + user.Key, null, _log);

        public void Dispose() { }
    }

    internal sealed class DefaultPersistentDataStoreFactory : IPersistentDataStoreFactory
    {
        public IPersistentDataStore CreatePersistentDataStore(LdClientContext context) =>
            new DefaultPersistentDataStore(context.BaseLogger.SubLogger(LogNames.DataStoreSubLog));
    }
}
