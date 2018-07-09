using System.Collections.Generic;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin.Tests
{
    internal class MockFlagCacheManager : IFlagCacheManager
    {
        private readonly IUserFlagCache _flagCache;

        public MockFlagCacheManager(IUserFlagCache flagCache)
        {
            _flagCache = flagCache;
        }

        public void CacheFlagsFromService(IDictionary<string, FeatureFlag> flags, User user)
        {
            _flagCache.CacheFlagsForUser(flags, user);
        }

        public FeatureFlag FlagForUser(string flagKey, User user)
        {
            var flags = FlagsForUser(user);
            FeatureFlag featureFlag;
            if (flags.TryGetValue(flagKey, out featureFlag))
            {
                return featureFlag;
            }

            return null;
        }

        public IDictionary<string, FeatureFlag> FlagsForUser(User user)
        {
            return _flagCache.RetrieveFlags(user);
        }

        public void RemoveFlagForUser(string flagKey, User user)
        {
            var flagsForUser = FlagsForUser(user);
            flagsForUser.Remove(flagKey);

            CacheFlagsFromService(flagsForUser, user);
        }

        public void UpdateFlagForUser(string flagKey, FeatureFlag featureFlag, User user)
        {
            var flagsForUser = FlagsForUser(user);
            flagsForUser[flagKey] = featureFlag;

            CacheFlagsFromService(flagsForUser, user);
        }
    }
}
