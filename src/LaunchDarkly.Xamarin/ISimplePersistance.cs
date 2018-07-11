namespace LaunchDarkly.Xamarin
{
    public interface ISimplePersistance
    {
        string GetValue(string key);
        void Save(string key, string value);
    }
}