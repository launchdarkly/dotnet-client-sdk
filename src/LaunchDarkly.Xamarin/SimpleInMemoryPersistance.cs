using System.Collections.Generic;

namespace LaunchDarkly.Xamarin
{
    internal class SimpleInMemoryPersistance : ISimplePersistance
    {
        IDictionary<string, string> map = new Dictionary<string, string>();

        public string GetValue(string key)
        {
            string value = null;
            map.TryGetValue(key, out value);
            return value;
        }

        public void Save(string key, string value)
        {
            map[key] = value;
        }
    }
}
