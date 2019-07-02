using System.Collections.Generic;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    internal interface IFlagCacheManager
    {
        void CacheFlagsFromService(IDictionary<string, FeatureFlag> flags, User user);
        FeatureFlag FlagForUser(string flagKey, User user);
        void UpdateFlagForUser(string flagKey, FeatureFlag featureFlag, User user);
        void RemoveFlagForUser(string flagKey, User user);
        IDictionary<string, FeatureFlag> FlagsForUser(User user);
    }
}