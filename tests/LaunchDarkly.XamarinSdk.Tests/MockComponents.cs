using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using LaunchDarkly.Client;
using LaunchDarkly.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Xamarin.Tests
{
    internal class MockBackgroundModeManager : IBackgroundModeManager
    {
        public event EventHandler<BackgroundModeChangedEventArgs> BackgroundModeChanged;

        public void UpdateBackgroundMode(bool isInBackground)
        {
            BackgroundModeChanged?.Invoke(this, new BackgroundModeChangedEventArgs(isInBackground));
        }
    }

    internal class MockConnectivityStateManager : IConnectivityStateManager
    {
        public Action<bool> ConnectionChanged { get; set; }

        public MockConnectivityStateManager(bool isOnline)
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
        internal const string GeneratedId = "fake-generated-id";

        private readonly string key;

        public MockDeviceInfo() : this(GeneratedId) { }

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
        public bool Offline = false;

        public void SetOffline(bool offline)
        {
            Offline = offline;
        }

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

        public void CacheFlagsFromService(IImmutableDictionary<string, FeatureFlag> flags, User user)
        {
            _flagCache.CacheFlagsForUser(flags, user);
        }

        public FeatureFlag FlagForUser(string flagKey, User user)
        {
            var flags = FlagsForUser(user);
            FeatureFlag featureFlag;
            if (flags != null && flags.TryGetValue(flagKey, out featureFlag))
            {
                return featureFlag;
            }

            return null;
        }

        public IImmutableDictionary<string, FeatureFlag> FlagsForUser(User user)
        {
            return _flagCache.RetrieveFlags(user);
        }

        public void RemoveFlagForUser(string flagKey, User user)
        {
            var flagsForUser = FlagsForUser(user);
            var updatedDict = flagsForUser.Remove(flagKey);

            CacheFlagsFromService(updatedDict, user);
        }

        public void UpdateFlagForUser(string flagKey, FeatureFlag featureFlag, User user)
        {
            var flagsForUser = FlagsForUser(user);
            var updatedDict = flagsForUser.SetItem(flagKey, featureFlag);

            CacheFlagsFromService(updatedDict, user);
        }
    }

    internal class MockPersistentStorage : IPersistentStorage
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
        private IFlagCacheManager _cacheManager;
        private User _user;
        private string _flagsJson;

        public User ReceivedUser => _user;

        public MockPollingProcessor(string flagsJson) : this(null, null, flagsJson) { }

        private MockPollingProcessor(IFlagCacheManager cacheManager, User user, string flagsJson)
        {
            _cacheManager = cacheManager;
            _user = user;
            _flagsJson = flagsJson;
        }

        public static Func<Configuration, IFlagCacheManager, User, IMobileUpdateProcessor> Factory(string flagsJson)
        {
            return (config, manager, user) => new MockPollingProcessor(manager, user, flagsJson);
        }

        public Func<Configuration, IFlagCacheManager, User, IMobileUpdateProcessor> AsFactory()
        {
            return (config, manager, user) =>
            {
                _cacheManager = manager;
                _user = user;
                return this;
            };
        }

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
            if (_cacheManager != null && _flagsJson != null)
            {
                _cacheManager.CacheFlagsFromService(JsonUtil.DecodeJson<ImmutableDictionary<string, FeatureFlag>>(_flagsJson), _user);
            }
            return Task.FromResult(true);
        }
    }

    internal class MockUpdateProcessorFromLambda : IMobileUpdateProcessor
    {
        private readonly User _user;
        private readonly Func<Task> _startFn;
        private bool _initialized;

        public MockUpdateProcessorFromLambda(User user, Func<Task> startFn)
        {
            _user = user;
            _startFn = startFn;
        }

        public Task<bool> Start()
        {
            return _startFn().ContinueWith<bool>(t =>
            {
                _initialized = true;
                return true;
            });
        }

        public bool Initialized() => _initialized;

        public void Dispose() { }
    }

    internal class MockUpdateProcessorThatNeverInitializes : IMobileUpdateProcessor
    {
        public static Func<Configuration, IFlagCacheManager, User, IMobileUpdateProcessor> Factory()
        {
            return (config, manager, user) => new MockUpdateProcessorThatNeverInitializes();
        }

        public bool IsRunning => false;

        public void Dispose()
        {
        }

        public bool Initialized()
        {
            return false;
        }

        public Task<bool> Start()
        {
            return new TaskCompletionSource<bool>().Task; // will never be completed
        }
    }
}
