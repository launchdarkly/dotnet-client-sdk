namespace LaunchDarkly.Xamarin
{
    internal interface ISimplePersistance
    {
        string GetValue(string key);
        void Save(string key, string value);
    }
}