using System.Collections.Immutable;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    internal sealed class InMemoryDataStore : IDataStore
    {
        private readonly object _writerLock = new object();
        private volatile ImmutableDictionary<string, ImmutableDictionary<string, ItemDescriptor>> _data =
            ImmutableDictionary<string, ImmutableDictionary<string, ItemDescriptor>>.Empty;

        public void Preload(User user) { }

        public void Init(User user, FullDataSet allData)
        {
            var newFlags = allData.Items.ToImmutableDictionary();
            lock (_writerLock)
            {
                _data = _data.SetItem(user.Key, newFlags);
            }
        }

        public ItemDescriptor? Get(User user, string key)
        {
            if (_data.TryGetValue(user.Key, out var flags))
            {
                return flags.TryGetValue(key, out var item) ? item : (ItemDescriptor?)null;
            }
            return null;
        }

        public FullDataSet? GetAll(User user) =>
            _data.TryGetValue(user.Key, out var flags) ? new FullDataSet(flags) : (FullDataSet?)null;

        public bool Upsert(User user, string key, ItemDescriptor item)
        {
            string userKey = user.Key;
            lock (_writerLock)
            {
                if (_data.TryGetValue(userKey, out var flags))
                {
                    if (!flags.TryGetValue(key, out var oldItem) || oldItem.Version < item.Version)
                    {
                        var newFlags = flags.SetItem(key, item);
                        _data = _data.SetItem(userKey, newFlags);
                        return true;
                    }
                    return false;
                }
                var allFlags = ImmutableDictionary.Create<string, ItemDescriptor>().SetItem(key, item);
                _data = _data.SetItem(userKey, allFlags);
                return true;
            }
        }

        public void Dispose() { }
    }
}
