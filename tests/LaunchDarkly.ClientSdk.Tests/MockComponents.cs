using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Client.Internal.DataSources;
using LaunchDarkly.Sdk.Client.Internal.Events;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;
using LaunchDarkly.Sdk.Client.PlatformSpecific;

using static LaunchDarkly.Sdk.Client.DataModel;
using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client
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
        public List<object> Events = new List<object>();
        public bool Offline = false;

        public void SetOffline(bool offline)
        {
            Offline = offline;
        }

        public void Flush() { }

        public void Dispose() { }

        public void RecordEvaluationEvent(EventProcessorTypes.EvaluationEvent e) =>
            Events.Add(e);

        public void RecordIdentifyEvent(EventProcessorTypes.IdentifyEvent e) =>
            Events.Add(e);

        public void RecordCustomEvent(EventProcessorTypes.CustomEvent e) =>
            Events.Add(e);

        public void RecordAliasEvent(EventProcessorTypes.AliasEvent e) =>
            Events.Add(e);
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

    internal class MockDataSourceFactoryFromLambda : IDataSourceFactory
    {
        private readonly Func<LdClientContext, IDataSourceUpdateSink, User, bool, IDataSource> _fn;

        internal MockDataSourceFactoryFromLambda(Func<LdClientContext, IDataSourceUpdateSink, User, bool, IDataSource> fn)
        {
            _fn = fn;
        }

        public IDataSource CreateDataSource(LdClientContext context, IDataSourceUpdateSink updateSink, User currentUser, bool inBackground) =>
            _fn(context, updateSink, currentUser, inBackground);
    }

    internal class SingleDataSourceFactory : MockDataSourceFactoryFromLambda
    {
        internal SingleDataSourceFactory(IDataSource instance) : base((c, up, u, bg) => instance) { }
    }

    internal class MockPollingProcessor : IDataSource
    {
        private IDataSourceUpdateSink _updateSink;
        private User _user;
        private string _flagsJson;

        public User ReceivedUser => _user;

        public MockPollingProcessor(string flagsJson) : this(null, null, flagsJson) { }

        private MockPollingProcessor(IDataSourceUpdateSink updateSink, User user, string flagsJson)
        {
            _updateSink = updateSink;
            _user = user;
            _flagsJson = flagsJson;
        }

        public static IDataSourceFactory Factory(string flagsJson) =>
            new MockDataSourceFactoryFromLambda((ctx, updates, user, bg) =>
                new MockPollingProcessor(updates, user, flagsJson));

        public IDataSourceFactory AsFactory() =>
            new MockDataSourceFactoryFromLambda((ctx, updates, user, bg) =>
            {
                this._updateSink = updates;
                this._user = user;
                return this;
            });

        public bool IsRunning
        {
            get;
            set;
        }

        public void Dispose()
        {
            IsRunning = false;
        }

        public bool Initialized => IsRunning;

        public Task<bool> Start()
        {
            IsRunning = true;
            if (_updateSink != null && _flagsJson != null)
            {
                _updateSink.Init(new FullDataSet(JsonUtil.DeserializeFlags(_flagsJson)), _user);
            }
            return Task.FromResult(true);
        }
    }

    internal class MockDataSourceFromLambda : IDataSource
    {
        private readonly User _user;
        private readonly Func<Task> _startFn;
        private bool _initialized;

        public MockDataSourceFromLambda(User user, Func<Task> startFn)
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

        public bool Initialized => _initialized;

        public void Dispose() { }
    }

    internal class MockUpdateProcessorThatNeverInitializes : IDataSource
    {
        public static IDataSourceFactory Factory() =>
            new SingleDataSourceFactory(new MockUpdateProcessorThatNeverInitializes());

        public bool IsRunning => false;

        public void Dispose() { }

        public bool Initialized => false;

        public Task<bool> Start()
        {
            return new TaskCompletionSource<bool>().Task; // will never be completed
        }
    }
}
