using System.Collections.Immutable;
using LaunchDarkly.Sdk.Client.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    internal sealed class NullPersistentDataStoreFactory : IPersistentDataStoreFactory
    {
        internal static readonly NullPersistentDataStoreFactory Instance = new NullPersistentDataStoreFactory();

        public IPersistentDataStore CreatePersistentDataStore(LdClientContext context) =>
            NullPersistentDataStore.Instance;
    }

    internal sealed class NullPersistentDataStore : IPersistentDataStore
    {
        internal static readonly NullPersistentDataStore Instance = new NullPersistentDataStore();

        public string GetValue(string storageNamespace, string key) => null;

        public void SetValue(string storageNamespace, string key, string value) { }

        public void Dispose() { }
    }
}
