using System;

namespace LaunchDarkly.Xamarin
{
    internal class SimpleMobileDevicePersistance : ISimplePersistance
    {
        public void Save(string key, string value)
        {
            LaunchDarkly.Xamarin.Preferences.Preferences.Set(key, value);
        }

        public string GetValue(string key)
        {
            return LaunchDarkly.Xamarin.Preferences.Preferences.Get(key, null);
        }
    }
}
