using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Xamarin.Internal.Interfaces;
using LaunchDarkly.Sdk.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Sdk.Xamarin.Internal.DataStores
{
    internal sealed class DefaultPersistentStorage : IPersistentStorage
    {
        private readonly Logger _log;

        internal DefaultPersistentStorage(Logger log)
        {
            _log = log;
        }

        public void Save(string key, string value) =>
            Preferences.Set(key, value, _log);

        public string GetValue(string key) =>
            Preferences.Get(key, null, _log);
    }
}
