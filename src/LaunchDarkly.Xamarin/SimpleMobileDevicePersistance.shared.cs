using System;

namespace LaunchDarkly.Xamarin
{
    internal class SimpleMobileDevicePersistance : ISimplePersistance
    {
        public void Save(string key, string value)
        {
            try
            {
                LaunchDarkly.Xamarin.Preferences.Preferences.Set(key, value);
            }
            catch (NotImplementedException) { }
        }

        public string GetValue(string key)
        {
            try
            {
                return LaunchDarkly.Xamarin.Preferences.Preferences.Get(key, null);
            }
            catch (NotImplementedException)
            {
                return null;
            }
        }
    }
}
