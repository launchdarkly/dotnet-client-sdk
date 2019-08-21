using LaunchDarkly.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Xamarin
{
    internal sealed class DefaultPersistentStorage : IPersistentStorage
    {
        public void Save(string key, string value)
        {
            Preferences.Set(key, value);
        }

        public string GetValue(string key)
        {
            return Preferences.Get(key, null);
        }
    }
}
