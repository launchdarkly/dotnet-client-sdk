using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Client.Internal.DataSources;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;
using LaunchDarkly.Sdk.Client.PlatformSpecific;
using LaunchDarkly.TestHelpers;
using Xunit;

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

    internal class MockDataSourceUpdateSink : IDataSourceUpdateSink
    {
        internal class ReceivedInit
        {
            public FullDataSet Data { get; set; }
            public User User { get; set; }
        }

        internal class ReceivedUpsert
        {
            public string Key { get; set; }
            public ItemDescriptor Data { get; set; }
            public User User { get; set; }
        }

        public readonly EventSink<object> Actions = new EventSink<object>();

        public void Init(FullDataSet data, User user) =>
            Actions.Add(null, new ReceivedInit { Data = data, User = user });

        public void Upsert(string key, ItemDescriptor data, User user) =>
            Actions.Add(null, new ReceivedUpsert { Key = key, Data = data, User = user });

        public FullDataSet ExpectInit(User user)
        {
            var action = Assert.IsType<ReceivedInit>(Actions.ExpectValue(TimeSpan.FromSeconds(5)));
            Assert.Equal(user, action.User);
            return action.Data;
        }

        public ItemDescriptor ExpectUpsert(User user, string key)
        {
            var action = Assert.IsType<ReceivedUpsert>(Actions.ExpectValue(TimeSpan.FromSeconds(5)));
            Assert.Equal(user, action.User);
            Assert.Equal(key, action.Key);
            return action.Data;
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

    internal class SingleEventProcessorFactory : IEventProcessorFactory
    {
        private readonly IEventProcessor _instance;

        public SingleEventProcessorFactory(IEventProcessor instance)
        {
            _instance = instance;
        }

        public IEventProcessor CreateEventProcessor(LdClientContext context) => _instance;
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

    internal class MockPersistentDataStore : IPersistentDataStore
    {
        private IDictionary<string, string> map = new Dictionary<string, string>();

        public void Dispose() { }

        public string GetAll(User user) =>
            map.TryGetValue(user.Key, out var value) ? value : null;

        public void Init(User user, string allData)
        {
            map[user.Key] = allData;
        }
    }

    internal class SinglePersistentDataStoreFactory : IPersistentDataStoreFactory
    {
        private readonly IPersistentDataStore _instance;

        public SinglePersistentDataStoreFactory(IPersistentDataStore instance)
        {
            _instance = instance;
        }

        public IPersistentDataStore CreatePersistentDataStore(LdClientContext context) =>
            _instance;
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
                _updateSink.Init(DataModelSerialization.DeserializeV1Schema(_flagsJson), _user);
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
