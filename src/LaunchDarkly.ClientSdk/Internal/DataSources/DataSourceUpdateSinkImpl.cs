using System.Collections.Generic;
using System.Collections.Immutable;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

using static LaunchDarkly.Sdk.Client.DataModel;
using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    internal sealed class DataSourceUpdateSinkImpl : IDataSourceUpdateSink
    {
        private readonly IDataStore _dataStore;
        private readonly IFlagChangedEventManager _flagChangedEventManager;
        private readonly object _lastValuesLock = new object();
        private volatile ImmutableDictionary<string, ImmutableDictionary<string, FeatureFlag>> _lastValues =
            ImmutableDictionary.Create<string, ImmutableDictionary<string, FeatureFlag>>();

        public DataSourceUpdateSinkImpl(
            IDataStore dataStore,
            IFlagChangedEventManager flagChangedEventManager
            )
        {
            _dataStore = dataStore;
            _flagChangedEventManager = flagChangedEventManager;
        }

        public void Init(FullDataSet data, User user)
        {
            _dataStore.Init(user, data);

            ImmutableDictionary<string, FeatureFlag> oldValues, newValues;
            lock (_lastValuesLock)
            {
                _lastValues.TryGetValue(user.Key, out oldValues);
                var builder = ImmutableDictionary.CreateBuilder<string, FeatureFlag>();
                foreach (var newEntry in data.Items)
                {
                    var newFlag = newEntry.Value.Item;
                    if (newFlag != null)
                    {
                        builder.Add(newEntry.Key, newFlag);
                    }
                }
                newValues = builder.ToImmutable();
                _lastValues = _lastValues.SetItem(user.Key, newValues);
            }

            if (oldValues != null)
            {
                List<FlagChangedEventArgs> events = new List<FlagChangedEventArgs>();

                foreach (var newEntry in newValues)
                {
                    var newFlag = newEntry.Value;
                    if (oldValues.TryGetValue(newEntry.Key, out var oldFlag))
                    {
                        if (newFlag.variation != oldFlag.variation)
                        {
                            events.Add(new FlagChangedEventArgs(newEntry.Key,
                                newFlag.value, oldFlag.value, false));
                        }
                    }
                    else
                    {
                        events.Add(new FlagChangedEventArgs(newEntry.Key,
                            newFlag.value, LdValue.Null, false));
                    }
                }
                foreach (var oldEntry in oldValues)
                {
                    if (!newValues.ContainsKey(oldEntry.Key))
                    {
                        events.Add(new FlagChangedEventArgs(oldEntry.Key,
                            LdValue.Null, oldEntry.Value.value, true));
                    }
                }
                foreach (var e in events)
                {
                    _flagChangedEventManager.FireEvent(e);
                }
            }
        }

        public void Upsert(string key, ItemDescriptor data, User user)
        {
            var updated = _dataStore.Upsert(user, key, data);
            if (!updated)
            {
                return;
            }

            FeatureFlag oldFlag = null;
            lock (_lastValuesLock)
            {
                _lastValues.TryGetValue(user.Key, out var oldValues);
                if (oldValues is null)
                {
                    // didn't have any flags for this user
                    var initValues = ImmutableDictionary.Create<string, FeatureFlag>();
                    if (data.Item != null)
                    {
                        initValues = initValues.SetItem(key, data.Item);
                    }
                    _lastValues = _lastValues.SetItem(user.Key, initValues);
                    return; // don't bother with change events if we had no previous data
                }
                oldValues.TryGetValue(key, out oldFlag);
                var newValues = data.Item is null ?
                    oldValues.Remove(key) : oldValues.SetItem(key, data.Item);
                _lastValues = _lastValues.SetItem(user.Key, newValues);
            }
            if (oldFlag?.variation != data.Item?.variation)
            {
                var eventArgs = new FlagChangedEventArgs(key,
                    data.Item?.value ?? LdValue.Null,
                    oldFlag?.value ?? LdValue.Null,
                    data.Item is null
                    );
                _flagChangedEventManager.FireEvent(eventArgs);
            }
        }
    }
}
