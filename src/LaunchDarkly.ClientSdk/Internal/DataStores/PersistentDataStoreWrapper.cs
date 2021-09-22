using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;
using LaunchDarkly.Sdk.Internal;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    // Internal implementation of combining an in-memory store with a persistent store.
    //
    // For details on the format of the serialized data we store, see DataModelSerialization.

    internal sealed class PersistentDataStoreWrapper : IDataStore
    {
        private readonly InMemoryDataStore _inMemoryStore;
        private readonly IPersistentDataStore _persistentStore;
        private readonly object _writerLock = new object();
        private readonly Logger _log;

        public PersistentDataStoreWrapper(
            InMemoryDataStore inMemoryStore,
            IPersistentDataStore persistentStore,
            Logger log
            )
        {
            _inMemoryStore = inMemoryStore;
            _persistentStore = persistentStore;
            _log = log;
        }

        public void Preload(User user)
        {
            lock (_writerLock)
            {
                if (_inMemoryStore.GetAll(user) is null)
                {
                    string serializedData = null;
                    try
                    {
                        serializedData = _persistentStore.GetAll(user);
                    }
                    catch (Exception e)
                    {
                        LogHelpers.LogException(_log, "Failed to read from persistent store", e);
                    }
                    if (serializedData is null)
                    {
                        return;
                    }
                    try
                    {
                        _inMemoryStore.Init(user, DataModelSerialization.DeserializeAll(serializedData));
                    }
                    catch (Exception e)
                    {
                        LogHelpers.LogException(_log, "Failed to deserialize data from persistent store", e);
                    }
                }
            }
        }

        public void Init(User user, FullDataSet allData)
        {
            lock (_writerLock)
            {
                _inMemoryStore.Init(user, allData);
                try
                {
                    _persistentStore.Init(user, DataModelSerialization.SerializeAll(allData));
                }
                catch (Exception e)
                {
                    LogHelpers.LogException(_log, "Failed to write to persistent store", e);
                }
            }
        }

        public ItemDescriptor? Get(User user, string key) =>
            _inMemoryStore.Get(user, key);

        public FullDataSet? GetAll(User user) =>
            _inMemoryStore.GetAll(user);

        public bool Upsert(User user, string key, ItemDescriptor data)
        {
            lock (_writerLock)
            {
                if (_inMemoryStore.Upsert(user, key, data))
                {
                    var allData = _inMemoryStore.GetAll(user);
                    if (allData != null)
                    {
                        try
                        {
                            _persistentStore.Init(user, DataModelSerialization.SerializeAll(allData.Value));
                        }
                        catch (Exception e)
                        {
                            LogHelpers.LogException(_log, "Failed to write to persistent store", e);
                        }
                    }
                    return true;
                }
                return false;
            }
        }

        public void Dispose()
        {
            _inMemoryStore.Dispose();
            _persistentStore.Dispose();
        }
    }
}
