using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin.Tests
{
    internal class MockConnectionManager : IConnectionManager
    {
        public Action<bool> ConnectionChanged;

        public MockConnectionManager(bool isOnline)
        {
            isConnected = isOnline;
        }

        bool isConnected;
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }

            set
            {
                isConnected = value;
            }
        }

        public void Connect(bool online)
        {
            IsConnected = online;
            ConnectionChanged?.Invoke(IsConnected);
        }
    }

    internal class MockDeviceInfo : IDeviceInfo
    {
        private readonly string key;

        public MockDeviceInfo(string key)
        {
            this.key = key;
        }

        public string UniqueDeviceId()
        {
            return key;
        }
    }

    internal class MockEventProcessor : IEventProcessor
    {
        public List<Event> Events = new List<Event>();

        public void SendEvent(Event e)
        {
            Events.Add(e);
        }

        public void Flush() { }

        public void Dispose() { }
    }

    internal class MockFeatureFlagRequestor : IFeatureFlagRequestor
    {
        private readonly string _jsonFlags;

        public MockFeatureFlagRequestor(string jsonFlags)
        {
            _jsonFlags = jsonFlags;
        }

        public void Dispose()
        {

        }

        public Task<WebResponse> FeatureFlagsAsync()
        {
            var response = new WebResponse(200, _jsonFlags, null);
            return Task.FromResult(response);
        }
    }

    internal class MockFlagCacheManager : IFlagCacheManager
    {
        private readonly IUserFlagCache _flagCache;

        public MockFlagCacheManager(IUserFlagCache flagCache)
        {
            _flagCache = flagCache;
        }

        public void CacheFlagsFromService(IDictionary<string, FeatureFlag> flags, User user)
        {
            _flagCache.CacheFlagsForUser(flags, user);
        }

        public FeatureFlag FlagForUser(string flagKey, User user)
        {
            var flags = FlagsForUser(user);
            FeatureFlag featureFlag;
            if (flags.TryGetValue(flagKey, out featureFlag))
            {
                return featureFlag;
            }

            return null;
        }

        public IDictionary<string, FeatureFlag> FlagsForUser(User user)
        {
            return _flagCache.RetrieveFlags(user);
        }

        public void RemoveFlagForUser(string flagKey, User user)
        {
            var flagsForUser = FlagsForUser(user);
            flagsForUser.Remove(flagKey);

            CacheFlagsFromService(flagsForUser, user);
        }

        public void UpdateFlagForUser(string flagKey, FeatureFlag featureFlag, User user)
        {
            var flagsForUser = FlagsForUser(user);
            flagsForUser[flagKey] = featureFlag;

            CacheFlagsFromService(flagsForUser, user);
        }
    }

    internal class MockPersister : ISimplePersistance
    {
        private IDictionary<string, string> map = new Dictionary<string, string>();

        public string GetValue(string key)
        {
            if (!map.ContainsKey(key))
                return null;

            return map[key];
        }

        public void Save(string key, string value)
        {
            map[key] = value;
        }
    }

    internal class MockPollingProcessor : IMobileUpdateProcessor
    {
        public bool IsRunning
        {
            get;
            set;
        }

        public void Dispose()
        {
            IsRunning = false;
        }

        public bool Initialized()
        {
            return IsRunning;
        }

        public Task<bool> Start()
        {
            IsRunning = true;
            return Task.FromResult(true);
        }
    }
}
