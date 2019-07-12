using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin
{
    internal interface IFeatureFlagListenerManager : IFlagListenerUpdater
    {
        void RegisterListener(IFeatureFlagListener listener, string flagKey);
        void UnregisterListener(IFeatureFlagListener listener, string flagKey);
        bool IsListenerRegistered(IFeatureFlagListener listener, string flagKey); // used for testing
    }

    internal interface IFlagListenerUpdater
    {
        void FlagWasUpdated(string flagKey, JToken value);
        void FlagWasDeleted(string flagKey);
    }
}