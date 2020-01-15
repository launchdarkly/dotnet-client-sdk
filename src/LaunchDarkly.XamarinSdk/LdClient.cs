using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Common;
using LaunchDarkly.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Xamarin
{
    /// <summary>
    /// A client for the LaunchDarkly API. Client instances are thread-safe. Your application should instantiate
    /// a single <c>LdClient</c> for the lifetime of their application.
    /// </summary>
    public sealed class LdClient : ILdClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LdClient));

        static readonly EventFactory _eventFactoryDefault = EventFactory.Default;
        static readonly EventFactory _eventFactoryWithReasons = EventFactory.DefaultWithReasons;

        static readonly object _createInstanceLock = new object();
        static volatile LdClient _instance;

        // Immutable client state
        readonly Configuration _config;
        readonly ConnectionManager _connectionManager;
        readonly IBackgroundModeManager _backgroundModeManager;
        readonly IDeviceInfo deviceInfo;
        readonly IConnectivityStateManager _connectivityStateManager;
        readonly IEventProcessor eventProcessor;
        readonly IFlagCacheManager flagCacheManager;
        internal readonly IFlagChangedEventManager flagChangedEventManager; // exposed for testing
        readonly IPersistentStorage persister;

        // Mutable client state (some state is also in the ConnectionManager)
        readonly ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim();
        volatile User _user;
        volatile bool _inBackground;

        /// <summary>
        /// The singleton instance used by your application throughout its lifetime. Once this exists, you cannot
        /// create a new client instance unless you first call <see cref="Dispose()"/> on this one.
        /// </summary>
        /// <remarks>
        /// Use the static factory methods <see cref="Init(Configuration, User, TimeSpan)"/> or
        /// <see cref="InitAsync(Configuration, User)"/> to set this <see cref="LdClient"/> instance.
        /// </remarks>
        public static LdClient Instance => _instance;

        /// <summary>
        /// The current version string of the SDK.
        /// </summary>
        public static Version Version => MobileClientEnvironment.Instance.Version;

        /// <summary>
        /// The <see cref="Configuration"/> instance used to set up the LdClient.
        /// </summary>
        public Configuration Config => _config;

        /// <summary>
        /// The current user for all SDK operations.
        /// </summary>
        /// <remarks>
        /// This is initially the user specified for <see cref="Init(Configuration, User, TimeSpan)"/> or
        /// <see cref="InitAsync(Configuration, User)"/>, but can be changed later with <see cref="Identify(User, TimeSpan)"/>
        /// or <see cref="IdentifyAsync(User)"/>.
        /// </remarks>
        public User User => LockUtils.WithReadLock(_stateLock, () => _user);

        /// <inheritdoc/>
        public bool Offline => _connectionManager.ForceOffline;

        /// <inheritdoc/>
        public bool Initialized => _connectionManager.Initialized;

        /// <summary>
        /// Indicates which platform the SDK is built for.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is mainly useful for debugging. It does not indicate which platform you are actually running on,
        /// but rather which variant of the SDK is currently in use.
        /// </para>
        /// <para>
        /// The <c>LaunchDarkly.XamarinSdk</c> package contains assemblies for multiple target platforms. In an Android
        /// or iOS application, you will normally be using the Android or iOS variant of the SDK; that is done
        /// automatically when you install the NuGet package. On all other platforms, you will get the .NET Standard
        /// variant.
        /// </para>
        /// <para>
        /// The basic features of the SDK are the same in all of these variants; the difference is in platform-specific
        /// behavior such as detecting when an application has gone into the background, detecting network connectivity,
        /// and ensuring that code is executed on the UI thread if applicable for that platform. Therefore, if you find
        /// that these platform-specific behaviors are not working correctly, you may want to check this property to
        /// make sure you are not for some reason running the .NET Standard SDK on a phone.
        /// </para>
        /// </remarks>
        public static PlatformType PlatformType => UserMetadata.PlatformType;

        /// <inheritdoc/>
        public event EventHandler<FlagChangedEventArgs> FlagChanged
        {
            add
            {
                flagChangedEventManager.FlagChanged += value;
            }
            remove
            {
                flagChangedEventManager.FlagChanged -= value;
            }
        }

        // private constructor prevents initialization of this class
        // without using WithConfigAnduser(config, user)
        LdClient() { }

        LdClient(Configuration configuration, User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));

            persister = Factory.CreatePersistentStorage(configuration);
            deviceInfo = Factory.CreateDeviceInfo(configuration);
            flagChangedEventManager = Factory.CreateFlagChangedEventManager(configuration);

            _user = DecorateUser(user);

            flagCacheManager = Factory.CreateFlagCacheManager(configuration, persister, flagChangedEventManager, User);

            _connectionManager = new ConnectionManager();
            _connectionManager.SetForceOffline(configuration.Offline);
            if (configuration.Offline)
            {
                Log.InfoFormat("Starting LaunchDarkly client in offline mode");
            }
            _connectionManager.SetUpdateProcessorFactory(
                Factory.CreateUpdateProcessorFactory(configuration, User, flagCacheManager, _inBackground),
                true
            );

            _connectivityStateManager = Factory.CreateConnectivityStateManager(configuration);
            _connectivityStateManager.ConnectionChanged += networkAvailable =>
            {
                Log.DebugFormat("Setting online to {0} due to a connectivity change event", networkAvailable);
                _ = _connectionManager.SetNetworkEnabled(networkAvailable);  // do not await the result
                eventProcessor.SetOffline(!networkAvailable || _connectionManager.ForceOffline);
            };
            var isConnected = _connectivityStateManager.IsConnected;
            _connectionManager.SetNetworkEnabled(isConnected);

            eventProcessor = Factory.CreateEventProcessor(configuration);
            eventProcessor.SetOffline(configuration.Offline || !isConnected);

            // Send an initial identify event, but only if we weren't explicitly set to be offline
            if (!configuration.Offline)
            {
                eventProcessor.SendEvent(_eventFactoryDefault.NewIdentifyEvent(User));
            }
            
            _backgroundModeManager = _config._backgroundModeManager ?? new DefaultBackgroundModeManager();
            _backgroundModeManager.BackgroundModeChanged += OnBackgroundModeChanged;
        }

        void Start(TimeSpan maxWaitTime)
        {
            var success = AsyncUtils.WaitSafely(() => _connectionManager.Start(), maxWaitTime);
            if (!success)
            {
                Log.WarnFormat("Client did not successfully initialize within {0} milliseconds.",
                    maxWaitTime.TotalMilliseconds);
            }
        }

        async Task StartAsync()
        {
            await _connectionManager.Start();
        }

        /// <summary>
        /// Creates a new <see cref="LdClient"/> singleton instance and attempts to initialize feature flags.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In offline mode, this constructor will return immediately. Otherwise, it will wait and block on
        /// the current thread until initialization and the first response from the LaunchDarkly service is
        /// returned, up to the specified timeout. If the timeout elapses, the returned instance will have
        /// an <see cref="Initialized"/> property of <see langword="false"/>.
        /// </para>
        /// <para>
        /// If you would rather this happen asynchronously, use <see cref="InitAsync(string, User)"/>. To
        /// specify additional configuration options rather than just the mobile key, use
        /// <see cref="Init(Configuration, User, TimeSpan)"/> or <see cref="InitAsync(Configuration, User)"/>.
        /// </para>
        /// <para>
        /// You must use one of these static factory methods to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </para>
        /// </remarks>
        /// <returns>the singleton <see cref="LdClient"/> instance</returns>
        /// <param name="mobileKey">the mobile key given to you by LaunchDarkly</param>
        /// <param name="user">the user needed for client operations (must not be <see langword="null"/>);
        /// if the user's <see cref="User.Key"/> is <see langword="null"/> and <see cref="User.Anonymous"/>
        /// is <see langword="true"/>, it will be assigned a key that uniquely identifies this device</param>
        /// <param name="maxWaitTime">the maximum length of time to wait for the client to initialize</param>
        public static LdClient Init(string mobileKey, User user, TimeSpan maxWaitTime)
        {
            var config = Configuration.Default(mobileKey);

            return Init(config, user, maxWaitTime);
        }

        /// <summary>
        /// Creates a new <see cref="LdClient"/> singleton instance and attempts to initialize feature flags asynchronously.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The returned task will yield the <see cref="LdClient"/> instance once the first response from
        /// the LaunchDarkly service is returned (or immediately if it is in offline mode).
        /// </para>
        /// <para>
        /// You must use one of these static factory methods to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </para>
        /// </remarks>
        /// <returns>the singleton <see cref="LdClient"/> instance</returns>
        /// <param name="mobileKey">the mobile key given to you by LaunchDarkly</param>
        /// <param name="user">the user needed for client operations (must not be <see langword="null"/>);
        /// if the user's <see cref="User.Key"/> is <see langword="null"/> and <see cref="User.Anonymous"/>
        /// is <see langword="true"/>, it will be assigned a key that uniquely identifies this device</param>
        public static async Task<LdClient> InitAsync(string mobileKey, User user)
        {
            var config = Configuration.Default(mobileKey);

            return await InitAsync(config, user);
        }

        /// <summary>
        /// Creates and returns a new LdClient singleton instance, then starts the workflow for 
        /// fetching Feature Flags.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In offline mode, this constructor will return immediately. Otherwise, it will wait and block on
        /// the current thread until initialization and the first response from the LaunchDarkly service is
        /// returned, up to the specified timeout. If the timeout elapses, the returned instance will have
        /// an <see cref="Initialized"/> property of <see langword="false"/>.
        /// </para>
        /// <para>
        /// If you would rather this happen asynchronously, use <see cref="InitAsync(Configuration, User)"/>.
        /// If you do not need to specify configuration options other than the mobile key, you can use
        /// <see cref="Init(string, User, TimeSpan)"/> or <see cref="InitAsync(string, User)"/>.
        /// </para>
        /// <para>
        /// You must use one of these static factory methods to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </para>
        /// </remarks>
        /// <returns>The singleton LdClient instance.</returns>
        /// <param name="config">The client configuration object</param>
        /// <param name="user">The user needed for client operations. Must not be null.
        /// If the user's Key is null, it will be assigned a key that uniquely identifies this device.</param>
        /// <param name="maxWaitTime">The maximum length of time to wait for the client to initialize.
        /// If this time elapses, the method will not throw an exception but will return the client in
        /// an uninitialized state.</param>
        public static LdClient Init(Configuration config, User user, TimeSpan maxWaitTime)
        {
            if (maxWaitTime.Ticks < 0 && maxWaitTime != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWaitTime));
            }

            var c = CreateInstance(config, user);
            c.Start(maxWaitTime);
            return c;
        }

        /// <summary>
        /// Creates and returns a new LdClient singleton instance, then starts the workflow for 
        /// fetching Feature Flags. This constructor should be used if you do not want to wait 
        /// for the IUpdateProcessor instance to finish initializing and receive the first response
        /// from the LaunchDarkly service.
        /// 
        /// This is the creation point for LdClient, you must use this static method or the more basic
        /// <see cref="InitAsync(string, User)"/> to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </summary>
        /// <returns>The singleton LdClient instance.</returns>
        /// <param name="config">The client configuration object</param>
        /// <param name="user">The user needed for client operations. Must not be null.
        /// If the user's Key is null, it will be assigned a key that uniquely identifies this device.</param>
        public static async Task<LdClient> InitAsync(Configuration config, User user)
        {
            var c = CreateInstance(config, user);
            await c.StartAsync();
            return c;
        }

        static LdClient CreateInstance(Configuration configuration, User user)
        {
            lock (_createInstanceLock)
            {
                if (_instance != null)
                {
                    throw new Exception("LdClient instance already exists.");
                }

                var c = new LdClient(configuration, user);
                _instance = c;
                Log.InfoFormat("Initialized LaunchDarkly Client {0}", Version);
                return c;
            }
        }

        /// <inheritdoc/>
        public bool SetOffline(bool value, TimeSpan maxWaitTime)
        {
            return AsyncUtils.WaitSafely(() => SetOfflineAsync(value), maxWaitTime);
        }

        /// <inheritdoc/>
        public async Task SetOfflineAsync(bool value)
        {
            eventProcessor.SetOffline(value || !_connectionManager.NetworkEnabled);
            await _connectionManager.SetForceOffline(value);
        }

        /// <inheritdoc/>
        public bool BoolVariation(string key, bool defaultValue = false)
        {
            return VariationInternal<bool>(key, LdValue.Of(defaultValue), LdValue.Convert.Bool, true, _eventFactoryDefault).Value;
        }

        /// <inheritdoc/>
        public EvaluationDetail<bool> BoolVariationDetail(string key, bool defaultValue = false)
        {
            return VariationInternal<bool>(key, LdValue.Of(defaultValue), LdValue.Convert.Bool, true, _eventFactoryWithReasons);
        }

        /// <inheritdoc/>
        public string StringVariation(string key, string defaultValue)
        {
            return VariationInternal<string>(key, LdValue.Of(defaultValue), LdValue.Convert.String, true, _eventFactoryDefault).Value;
        }

        /// <inheritdoc/>
        public EvaluationDetail<string> StringVariationDetail(string key, string defaultValue)
        {
            return VariationInternal<string>(key, LdValue.Of(defaultValue), LdValue.Convert.String, true, _eventFactoryWithReasons);
        }

        /// <inheritdoc/>
        public float FloatVariation(string key, float defaultValue = 0)
        {
            return VariationInternal<float>(key, LdValue.Of(defaultValue), LdValue.Convert.Float, true, _eventFactoryDefault).Value;
        }

        /// <inheritdoc/>
        public EvaluationDetail<float> FloatVariationDetail(string key, float defaultValue = 0)
        {
            return VariationInternal<float>(key, LdValue.Of(defaultValue), LdValue.Convert.Float, true, _eventFactoryWithReasons);
        }

        /// <inheritdoc/>
        public int IntVariation(string key, int defaultValue = 0)
        {
            return VariationInternal(key, LdValue.Of(defaultValue), LdValue.Convert.Int, true, _eventFactoryDefault).Value;
        }

        /// <inheritdoc/>
        public EvaluationDetail<int> IntVariationDetail(string key, int defaultValue = 0)
        {
            return VariationInternal(key, LdValue.Of(defaultValue), LdValue.Convert.Int, true, _eventFactoryWithReasons);
        }

        /// <inheritdoc/>
        public LdValue JsonVariation(string key, LdValue defaultValue)
        {
            return VariationInternal(key, defaultValue, LdValue.Convert.Json, false, _eventFactoryDefault).Value;
        }

        /// <inheritdoc/>
        public EvaluationDetail<LdValue> JsonVariationDetail(string key, LdValue defaultValue)
        {
            return VariationInternal(key, defaultValue, LdValue.Convert.Json, false, _eventFactoryWithReasons);
        }

        EvaluationDetail<T> VariationInternal<T>(string featureKey, LdValue defaultJson, LdValue.Converter<T> converter, bool checkType, EventFactory eventFactory)
        {
            T defaultValue = converter.ToType(defaultJson);

            EvaluationDetail<T> errorResult(EvaluationErrorKind kind) =>
                new EvaluationDetail<T>(defaultValue, null, EvaluationReason.ErrorReason(kind));

            var flag = flagCacheManager.FlagForUser(featureKey, User);
            if (flag == null)
            {
                if (!Initialized)
                {
                    Log.Warn("LaunchDarkly client has not yet been initialized. Returning default value");
                    SendEventIfOnline(eventFactory.NewUnknownFeatureRequestEvent(featureKey, User, defaultJson,
                        EvaluationErrorKind.CLIENT_NOT_READY));
                    return errorResult(EvaluationErrorKind.CLIENT_NOT_READY);
                }
                else
                {
                    Log.InfoFormat("Unknown feature flag {0}; returning default value", featureKey);
                    SendEventIfOnline(eventFactory.NewUnknownFeatureRequestEvent(featureKey, User, defaultJson,
                        EvaluationErrorKind.FLAG_NOT_FOUND));
                    return errorResult(EvaluationErrorKind.FLAG_NOT_FOUND);
                }
            }
            else
            {
                if (!Initialized)
                {
                    Log.Warn("LaunchDarkly client has not yet been initialized. Returning cached value");
                }
            }

            FeatureFlagEvent featureFlagEvent = new FeatureFlagEvent(featureKey, flag);
            EvaluationDetail<T> result;
            LdValue valueJson;
            if (flag.value.IsNull)
            {
                valueJson = defaultJson;
                result = new EvaluationDetail<T>(defaultValue, flag.variation, flag.reason);
            }
            else
            {
                if (checkType && !defaultJson.IsNull && flag.value.Type != defaultJson.Type)
                {
                    valueJson = defaultJson;
                    result = new EvaluationDetail<T>(defaultValue, null, EvaluationReason.ErrorReason(EvaluationErrorKind.WRONG_TYPE));
                }
                else
                {
                    valueJson = flag.value;
                    result = new EvaluationDetail<T>(converter.ToType(flag.value), flag.variation, flag.reason);
                }
            }
            var featureEvent = eventFactory.NewFeatureRequestEvent(featureFlagEvent, User,
                new EvaluationDetail<LdValue>(valueJson, flag.variation, flag.reason), defaultJson);
            SendEventIfOnline(featureEvent);
            return result;
        }

        private void SendEventIfOnline(Event e)
        {
            if (!_connectionManager.ForceOffline)
            {
                eventProcessor.SendEvent(e);
            }
        }

        /// <inheritdoc/>
        public IDictionary<string, LdValue> AllFlags()
        {
            return flagCacheManager.FlagsForUser(User)
                                    .ToDictionary(p => p.Key, p => p.Value.value);
        }

        /// <inheritdoc/>
        public void Track(string eventName, LdValue data, double metricValue)
        {
            eventProcessor.SendEvent(_eventFactoryDefault.NewCustomEvent(eventName, User, data, metricValue));
        }

        /// <inheritdoc/>
        public void Track(string eventName, LdValue data)
        {
            SendEventIfOnline(_eventFactoryDefault.NewCustomEvent(eventName, User, data));
        }

        /// <inheritdoc/>
        public void Track(string eventName)
        {
            Track(eventName, LdValue.Null);
        }

        /// <inheritdoc/>
        public void Flush()
        {
            eventProcessor.Flush(); // eventProcessor will ignore this if it is offline
        }

        /// <inheritdoc/>
        public bool Identify(User user, TimeSpan maxWaitTime)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return AsyncUtils.WaitSafely(() => IdentifyAsync(user), maxWaitTime);
        }

        /// <inheritdoc/>
        public async Task<bool> IdentifyAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            User newUser = DecorateUser(user);

            LockUtils.WithWriteLock(_stateLock, () =>
            {
                _user = newUser;
            });

            SendEventIfOnline(_eventFactoryDefault.NewIdentifyEvent(newUser));

            return await _connectionManager.SetUpdateProcessorFactory(
                Factory.CreateUpdateProcessorFactory(_config, newUser, flagCacheManager, _inBackground),
                true
            );
        }

        User DecorateUser(User user)
        {
            IUserBuilder buildUser = null;
            if (UserMetadata.DeviceName != null)
            {
                if (buildUser is null)
                {
                    buildUser = User.Builder(user);
                }
                buildUser.Custom("device", UserMetadata.DeviceName);
            }
            if (UserMetadata.OSName != null)
            {
                if (buildUser is null)
                {
                    buildUser = User.Builder(user);
                }
                buildUser.Custom("os", UserMetadata.OSName);
            }
            // If you pass in a user with a null or blank key, one will be assigned to them.
            if (String.IsNullOrEmpty(user.Key))
            {
                if (buildUser is null)
                {
                    buildUser = User.Builder(user);
                }
                buildUser.Key(deviceInfo.UniqueDeviceId()).Anonymous(true);
            }
            return buildUser is null ? user : buildUser.Build();
        }

        /// <summary>
        /// Permanently shuts down the SDK client.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method closes all network collections, shuts down all background tasks, and releases any other
        /// resources being held by the SDK.
        /// </para>
        /// <para>
        /// If there are any pending analytics events, and if the SDK is online, it attempts to deliver the events
        /// to LaunchDarkly before closing.
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                Log.InfoFormat("Shutting down the LaunchDarkly client");

                _backgroundModeManager.BackgroundModeChanged -= OnBackgroundModeChanged;
                _connectionManager.Dispose();
                eventProcessor.Dispose();

                // Reset the static Instance to null *if* it was referring to this instance
                DetachInstance();
            }
        }

        internal void DetachInstance() // exposed for testing
        {
            Interlocked.CompareExchange(ref _instance, null, this);
        }

        internal void OnBackgroundModeChanged(object sender, BackgroundModeChangedEventArgs args)
        {
            _ = OnBackgroundModeChangedAsync(sender, args); // do not wait for the result
        }

        internal async Task OnBackgroundModeChangedAsync(object sender, BackgroundModeChangedEventArgs args)
        {
            var goingIntoBackground = args.IsInBackground;
            var wasInBackground = LockUtils.WithWriteLock(_stateLock, () =>
            {
                var oldValue = _inBackground;
                _inBackground = goingIntoBackground;
                return oldValue;
            });
            if (goingIntoBackground == wasInBackground)
            {
                return;
            }
            Log.DebugFormat("Background mode is changing to {0}", goingIntoBackground);
            if (goingIntoBackground)
            {
                if (!Config.EnableBackgroundUpdating)
                {
                    Log.Debug("Background updating is disabled");
                    await _connectionManager.SetUpdateProcessorFactory(null, false);
                    return;
                }
                Log.Debug("Background updating is enabled, starting polling processor");
            }
            await _connectionManager.SetUpdateProcessorFactory(
                Factory.CreateUpdateProcessorFactory(_config, User, flagCacheManager, goingIntoBackground),
                false  // don't reset initialized state because the user is still the same
            );
        }
    }
}
