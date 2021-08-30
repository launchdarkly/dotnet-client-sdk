using System.Collections.Concurrent;
using System.Collections.Immutable;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal.DataStores
{
    internal sealed class UserFlagInMemoryCache : IUserFlagCache
    {
        // For each known user key, store a map of their flags. This is a write-through cache - updates will always
        // go to UserFlagDeviceCache as well. The inner dictionaries are immutable; updates are done by updating
        // the whole thing (that is safe because updates are only ever done from the streaming/polling thread, and
        // since updates should be relatively infrequent, it's not very expensive).

        private readonly ConcurrentDictionary<string, IImmutableDictionary<string, FeatureFlag>> _allData =
            new ConcurrentDictionary<string, IImmutableDictionary<string, FeatureFlag>>();

        void IUserFlagCache.CacheFlagsForUser(IImmutableDictionary<string, FeatureFlag> flags, User user)
        {
            _allData[user.Key] = flags;
        }

        IImmutableDictionary<string, FeatureFlag> IUserFlagCache.RetrieveFlags(User user)
        {
            if (_allData.TryGetValue(user.Key, out var flags))
            {
                return flags;
            }
            return ImmutableDictionary.Create<string, FeatureFlag>();
        }
    }
}
