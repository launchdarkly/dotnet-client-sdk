using System;
using Xamarin.Essentials;

namespace LaunchDarkly.Xamarin
{
    internal class SimpleMobileDevicePersistance : ISimplePersistance
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
