using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Client.Internal.DataSources;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;
using LaunchDarkly.Sdk.Client.PlatformSpecific;
using LaunchDarkly.Sdk.Internal.Events;
using LaunchDarkly.TestHelpers;
using Xunit;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Client
{
    // Even more so than in the server-side SDK tests, we rely on our own concrete mock
    // implementations of our component interfaces rather than using Moq to create mocks
    // dynamically, because runtime differences cause Moq to fail on some mobile platforms.

    internal static class MockComponentExtensions
    {
        public static IDataSourceFactory AsSingletonFactory(this IDataSource instance) =>
            new SingleDataSourceFactory { Instance = instance };

        public static IEventProcessorFactory AsSingletonFactory(this IEventProcessor instance) =>
            new SingleEventProcessorFactory { Instance = instance };

        public static IPersistentDataStoreFactory AsSingletonFactory(this IPersistentDataStore instance) =>
            new SinglePersistentDataStoreFactory { Instance = instance };

        private class SingleDataSourceFactory : IDataSourceFactory
        {
            public IDataSource Instance { get; set; }
            public IDataSource CreateDataSource(LdClientContext context, IDataSourceUpdateSink updateSink,
                User currentUser, bool inBackground) => Instance;
        }

        private class SingleEventProcessorFactory : IEventProcessorFactory
        {
            public IEventProcessor Instance { get; set; }
            public IEventProcessor CreateEventProcessor(LdClientContext context) => Instance;
        }

        private class SinglePersistentDataStoreFactory : IPersistentDataStoreFactory
        {
            public IPersistentDataStore Instance { get; set; }
            public IPersistentDataStore CreatePersistentDataStore(LdClientContext context) => Instance;
        }
    }

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

        internal class ReceivedStatusUpdate
        {
            public DataSourceState State { get; set; }
            public DataSourceStatus.ErrorInfo? Error { get; set; }
        }

        public readonly EventSink<object> Actions = new EventSink<object>();

        public void Init(User user, FullDataSet data) =>
            Actions.Enqueue(new ReceivedInit { Data = data, User = user });

        public void Upsert(User user, string key, ItemDescriptor data) =>
            Actions.Enqueue(new ReceivedUpsert { Key = key, Data = data, User = user });

        public void UpdateStatus(DataSourceState newState, DataSourceStatus.ErrorInfo? newError) =>
            Actions.Enqueue(new ReceivedStatusUpdate { State = newState, Error = newError });

        public FullDataSet ExpectInit(User user)
        {
            var action = Assert.IsType<ReceivedInit>(Actions.ExpectValue(TimeSpan.FromSeconds(5)));
            AssertHelpers.UsersEqual(user, action.User);
            return action.Data;
        }

        public ItemDescriptor ExpectUpsert(User user, string key)
        {
            var action = Assert.IsType<ReceivedUpsert>(Actions.ExpectValue(TimeSpan.FromSeconds(5)));
            Assert.Equal(user, action.User);
            Assert.Equal(key, action.Key);
            return action.Data;
        }

        public DataSourceStatus ExpectStatusUpdate()
        {
            var action = Assert.IsType<ReceivedStatusUpdate>(Actions.ExpectValue(TimeSpan.FromSeconds(5)));
            return new DataSourceStatus { State = action.State, LastError = action.Error };
        }

        public void ExpectNoMoreActions() => Actions.ExpectNoValue();
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

    internal class MockDiagnosticStore : IDiagnosticStore
    {
        internal struct StreamInit
        {
            internal DateTime Timestamp;
            internal TimeSpan Duration;
            internal bool Failed;
        }

        internal readonly EventSink<StreamInit> StreamInits = new EventSink<StreamInit>();

        public DateTime DataSince => DateTime.Now;

        public DiagnosticEvent? InitEvent => null;

        public DiagnosticEvent? PersistedUnsentEvent => null;

        public void AddStreamInit(DateTime timestamp, TimeSpan duration, bool failed) =>
            StreamInits.Enqueue(new StreamInit { Timestamp = timestamp, Duration = duration, Failed = failed });

        public DiagnosticEvent CreateEventAndReset() => new DiagnosticEvent();

        public void IncrementDeduplicatedUsers() { }

        public void IncrementDroppedEvents() { }

        public void RecordEventsInBatch(long eventsInBatch) { }
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

    public class MockEventSender : IEventSender
    {
        public BlockingCollection<Params> Calls = new BlockingCollection<Params>();
        public EventDataKind? FilterKind = null;

        public void Dispose() { }

        public struct Params
        {
            public EventDataKind Kind;
            public string Data;
            public int EventCount;
        }

        public Task<EventSenderResult> SendEventDataAsync(EventDataKind kind, string data, int eventCount)
        {
            if (!FilterKind.HasValue || kind == FilterKind.Value)
            {
                Calls.Add(new Params { Kind = kind, Data = data, EventCount = eventCount });
            }
            return Task.FromResult(new EventSenderResult(DeliveryStatus.Succeeded, null));
        }

        public Params RequirePayload()
        {
            Params result;
            if (!Calls.TryTake(out result, TimeSpan.FromSeconds(5)))
            {
                throw new System.Exception("did not receive an event payload");
            }
            return result;
        }

        public void RequireNoPayloadSent(TimeSpan timeout)
        {
            Params result;
            if (Calls.TryTake(out result, timeout))
            {
                throw new System.Exception("received an unexpected event payload");
            }
        }
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
        private Dictionary<(string, string), string> _map = new Dictionary<(string, string), string>();

        public void Dispose() { }

        public string GetValue(string storageNamespace, string key) =>
            _map.TryGetValue((storageNamespace, key), out var value) ? value : null;

        public void SetValue(string storageNamespace, string key, string value)
        {
            if (value is null)
            {
                _map.Remove((storageNamespace, key));
            }
            else
            {
                _map[(storageNamespace, key)] = value;
            }
        }

        public ImmutableList<string> GetKeys(string storageNamespace) =>
            _map.Where(kv => kv.Key.Item1 == storageNamespace).Select(kv => kv.Value).ToImmutableList();

        private PersistentDataStoreWrapper WithWrapper(string mobileKey) =>
            new PersistentDataStoreWrapper(this, mobileKey, Logs.None.Logger(""));

        internal void SetupUserData(string mobileKey, string userKey, FullDataSet data) =>
            WithWrapper(mobileKey).SetUserData(Base64.Sha256Hash(userKey), data);

        internal FullDataSet? InspectUserData(string mobileKey, string userKey) =>
            WithWrapper(mobileKey).GetUserData(Base64.Sha256Hash(userKey));

        internal UserIndex InspectUserIndex(string mobileKey) =>
            WithWrapper(mobileKey).GetIndex();
    }

    internal class CapturingDataSourceFactory : IDataSourceFactory
    {
        internal IDataSourceUpdateSink UpdateSink;

        public IDataSource CreateDataSource(LdClientContext context, IDataSourceUpdateSink updateSink, User currentUser, bool inBackground)
        {
            UpdateSink = updateSink;
            return new MockDataSourceThatNeverInitializes();
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

    internal class MockPollingProcessor : IDataSource
    {
        private IDataSourceUpdateSink _updateSink;
        private User _user;
        private FullDataSet? _data;

        public User ReceivedUser => _user;

        public MockPollingProcessor(FullDataSet? data) : this(null, null, data) { }

        private MockPollingProcessor(IDataSourceUpdateSink updateSink, User user, FullDataSet? data)
        {
            _updateSink = updateSink;
            _user = user;
            _data = data;
        }

        public static IDataSourceFactory Factory(FullDataSet? data) =>
            new MockDataSourceFactoryFromLambda((ctx, updates, user, bg) =>
                new MockPollingProcessor(updates, user, data));

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
            if (_updateSink != null && _data != null)
            {
                _updateSink.Init(_user, _data.Value);
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

    internal class MockDataSource : IDataSource
    {
        public bool IsRunning => true;

        public void Dispose() { }

        public bool Initialized => true;

        public Task<bool> Start() => Task.FromResult(true);
    }

    internal class MockDataSourceThatNeverInitializes : IDataSource
    {
        public bool IsRunning => false;

        public void Dispose() { }

        public bool Initialized => false;

        public Task<bool> Start() => new TaskCompletionSource<bool>().Task; // will never be completed
    }
}
