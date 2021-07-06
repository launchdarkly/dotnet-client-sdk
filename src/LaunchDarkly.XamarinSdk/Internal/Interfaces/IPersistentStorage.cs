namespace LaunchDarkly.Sdk.Xamarin.Internal.Interfaces
{
    internal interface IPersistentStorage
    {
        string GetValue(string key);
        void Save(string key, string value);
    }

    internal class NullPersistentStorage : IPersistentStorage
    {
        public string GetValue(string key) => null;
        public void Save(string key, string value) { }
    }
}