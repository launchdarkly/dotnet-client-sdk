using System.Collections.Immutable;

using static LaunchDarkly.Sdk.Client.DataModel;

namespace LaunchDarkly.Sdk.Client.Internal.Interfaces
{
    internal interface IFlagCacheManager
    {
        void CacheFlagsFromService(IImmutableDictionary<string, FeatureFlag> flags, User user);
        FeatureFlag FlagForUser(string flagKey, User user);
        void UpdateFlagForUser(string flagKey, FeatureFlag featureFlag, User user);
        void RemoveFlagForUser(string flagKey, User user);
        IImmutableDictionary<string, FeatureFlag> FlagsForUser(User user);
    }
}
