namespace LaunchDarkly.Xamarin
{
    internal class SimpleMobileDevicePersistance : ISimplePersistance
    {
        public void Save(string key, string value)
        {
            try
            {
                Preferences.Set(key, value);
            }
            catch (NotImplementedInReferenceAssemblyException) { }
        }

        public string GetValue(string key)
        {
            try
            {
                return Preferences.Get(key, null);
            }
            catch (NotImplementedInReferenceAssemblyException)
            {
                return null;
            }
        }
    }
}
