using System.Collections.Concurrent;
using System.Collections.Generic;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    internal sealed class UserFlagInMemoryCache : IUserFlagCache
    {
        // A map of the key (user.Key) and their featureFlags
        readonly ConcurrentDictionary<string, string> JSONMap =
            new ConcurrentDictionary<string, string>();

        void IUserFlagCache.CacheFlagsForUser(IDictionary<string, FeatureFlag> flags, User user)
        {
            var jsonString = JsonUtil.EncodeJson(flags);
            JSONMap[user.Key] = jsonString;
        }

        IDictionary<string, FeatureFlag> IUserFlagCache.RetrieveFlags(User user)
        {
            string json;
            if (JSONMap.TryGetValue(user.Key, out json))
            {
                return JsonUtil.DecodeJson<IDictionary<string, FeatureFlag>>(json);
            }

            return new Dictionary<string, FeatureFlag>();
        }
    }
}
