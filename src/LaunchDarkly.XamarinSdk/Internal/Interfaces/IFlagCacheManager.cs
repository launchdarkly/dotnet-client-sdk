using System.Collections.Immutable;

namespace LaunchDarkly.Sdk.Xamarin.Internal.Interfaces
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