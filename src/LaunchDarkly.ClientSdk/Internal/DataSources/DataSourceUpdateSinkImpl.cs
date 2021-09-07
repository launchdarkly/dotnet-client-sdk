using System.Collections.Immutable;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

using static LaunchDarkly.Sdk.Client.DataModel;
using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    internal sealed class DataSourceUpdateSinkImpl : IDataSourceUpdateSink
    {
        private readonly IFlagCacheManager _dataStore;

        public DataSourceUpdateSinkImpl(IFlagCacheManager flagCacheManager)
        {
            _dataStore = flagCacheManager;
        }

        public void Init(FullDataSet data, User user)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, FeatureFlag>();
            foreach (var entry in data.Flags)
            {
                if (entry.Value != null)
                {
                    builder.Add(entry.Key, entry.Value);
                }
            }
            _dataStore.CacheFlagsFromService(builder.ToImmutableDictionary(), user);
        }

        public void Upsert(string key, int version, FeatureFlag data, User user)
        {
            var oldItem = _dataStore.FlagForUser(key, user);
            if (oldItem != null && oldItem.version >= version)
            {
                return;
            }
            _dataStore.UpdateFlagForUser(key, data, user);
            // Eventually we should make this class responsible for sending flag change events,
            // to decouple that behavior from the storage mechanism, but currently that is
            // implemented within FlagCacheManager.
        }

        public void Delete(string key, int version, User user)
        {
            var oldItem = _dataStore.FlagForUser(key, user);
            if (oldItem == null || oldItem.version >= version)
            {
                return;
            }
            // Currently we are not storing a "tombstone" for deletions, so it is possible for
            // an out-of-order update after a delete to recreate the flag. We need to extend the
            // data store implementation to allow tombstones.
            _dataStore.RemoveFlagForUser(key, user);
        }
    }
}
