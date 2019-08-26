using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Common;
using LaunchDarkly.Xamarin.PlatformSpecific;
using Newtonsoft.Json.Linq;

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
        readonly IDeviceInfo deviceInfo;
        readonly IConnectivityStateManager _connectivityStateManager;
        readonly IEventProcessor eventProcessor;
        readonly IFlagCacheManager flagCacheManager;
        internal readonly IFlagChangedEventManager flagChangedEventManager; // exposed for testing
        readonly IPersistentStorage persister;

        // Mutable client state (some state is also in the ConnectionManager)
        readonly ReaderWriterLockSlim _userLock = new ReaderWriterLockSlim();
        volatile User _user;
        volatile bool _inBackground;

        /// <summary>
        /// The singleton instance used by your application throughout its lifetime. Once this exists, you cannot
        /// create a new client instance unless you first call <see cref="Dispose()"/> on this one.
        /// </summary>
        /// <remarks>
        /// Use the static factory methods <see cref="Init(Configuration, User, TimeSpan)"/> or
        /// <see cref="InitAsync(Configuration, User)"/> to set this <c>LdClient</c> instance.
        /// </remarks>
        public static LdClient Instance => _instance;

        /// <summary>
        /// Returns the current version number of the LaunchDarkly client.
        /// </summary>
        public static Version Version => MobileClientEnvironment.Instance.Version;

        /// <summary>
        /// The Configuration instance used to setup the LdClient.
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
        public User User => LockUtils.WithReadLock(_userLock, () => _user);

        /// <see cref="ILdClient.Offline"/>
        public bool Offline => _connectionManager.ForceOffline;

        /// <see cref="ILdClient.Initialized"/>
        public bool Initialized => _connectionManager.Initialized;

        /// <summary>
        /// Indicates which platform the SDK is built for.
        /// </summary>
        /// <remarks>
        /// This property is mainly useful for debugging. It does not indicate which platform you are actually running on,
        /// but rather which variant of the SDK is currently in use.
        /// 
        /// The <c>LaunchDarkly.XamarinSdk</c> package contains assemblies for multiple target platforms. In an Android
        /// or iOS application, you will normally be using the Android or iOS variant of the SDK; that is done
        /// automatically when you install the NuGet package. On all other platforms, you will get the .NET Standard
        /// variant.
        ///
        /// The basic features of the SDK are the same in all of these variants; the difference is in platform-specific
        /// behavior such as detecting when an application has gone into the background, detecting network connectivity,
        /// and ensuring that code is executed on the UI thread if applicable for that platform. Therefore, if you find
        /// that these platform-specific behaviors are not working correctly, you may want to check this property to
        /// make sure you are not for some reason running the .NET Standard SDK on a phone.
        /// </remarks>
        public static PlatformType PlatformType => UserMetadata.PlatformType;

        /// <see cref="ILdClient.FlagChanged"/>
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
            eventProcessor = Factory.CreateEventProcessor(configuration);

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

            eventProcessor.SendEvent(_eventFactoryDefault.NewIdentifyEvent(User));

            _connectivityStateManager = Factory.CreateConnectivityStateManager(configuration);
            _connectivityStateManager.ConnectionChanged += networkAvailable =>
            {
                Log.DebugFormat("Setting online to {0} due to a connectivity change event", networkAvailable);
                _ = _connectionManager.SetNetworkEnabled(networkAvailable);  // do not await the result
            };
            _connectionManager.SetNetworkEnabled(_connectivityStateManager.IsConnected);

            BackgroundDetection.BackgroundModeChanged += OnBackgroundModeChanged;
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
        /// Creates and returns a new LdClient singleton instance, then starts the workflow for 
        /// fetching feature flags.
        /// 
        /// This constructor will wait and block on the current thread until initialization and the
        /// first response from the LaunchDarkly service is returned, up to the specified timeout.
        /// If you would rather this happen in an async fashion you can use <see cref="InitAsync(string, User)"/>.
        /// 
        /// This is the creation point for LdClient, you must use this static method or the more specific
        /// <see cref="Init(Configuration, User, TimeSpan)"/> to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </summary>
        /// <returns>The singleton LdClient instance.</returns>
        /// <param name="mobileKey">The mobile key given to you by LaunchDarkly.</param>
        /// <param name="user">The user needed for client operations. Must not be null.
        /// If the user's Key is null, it will be assigned a key that uniquely identifies this device.</param>
        /// <param name="maxWaitTime">The maximum length of time to wait for the client to initialize.
        /// If this time elapses, the method will not throw an exception but will return the client in
        /// an uninitialized state.</param>
        public static LdClient Init(string mobileKey, User user, TimeSpan maxWaitTime)
        {
            var config = Configuration.Default(mobileKey);

            return Init(config, user, maxWaitTime);
        }

        /// <summary>
        /// Creates and returns a new LdClient singleton instance, then starts the workflow for 
        /// fetching feature flags. This constructor should be used if you do not want to wait 
        /// for the client to finish initializing and receive the first response
        /// from the LaunchDarkly service.
        /// 
        /// This is the creation point for LdClient, you must use this static method or the more specific
        /// <see cref="InitAsync(Configuration, User)"/> to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </summary>
        /// <returns>The singleton LdClient instance.</returns>
        /// <param name="mobileKey">The mobile key given to you by LaunchDarkly.</param>
        /// <param name="user">The user needed for client operations. Must not be null.
        /// If the user's Key is null, it will be assigned a key that uniquely identifies this device.</param>
        public static async Task<LdClient> InitAsync(string mobileKey, User user)
        {
            var config = Configuration.Default(mobileKey);

            return await InitAsync(config, user);
        }

        /// <summary>
        /// Creates and returns a new LdClient singleton instance, then starts the workflow for 
        /// fetching Feature Flags.
        /// 
        /// This constructor will wait and block on the current thread until initialization and the
        /// first response from the LaunchDarkly service is returned, up to the specified timeout.
        /// If you would rather this happen in an async fashion you can use <see cref="InitAsync(Configuration, User)"/>.
        /// 
        /// This is the creation point for LdClient, you must use this static method or the more basic
        /// <see cref="Init(string, User, TimeSpan)"/> to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </summary>
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

        /// <see cref="ILdClient.SetOffline(bool, TimeSpan)"/>
        public bool SetOffline(bool value, TimeSpan maxWaitTime)
        {
            return AsyncUtils.WaitSafely(() => SetOfflineAsync(value), maxWaitTime);
        }

        /// <see cref="ILdClient.SetOfflineAsync(bool)"/>
        public async Task SetOfflineAsync(bool value)
        {
            await _connectionManager.SetForceOffline(value);
        }

        /// <see cref="ILdClient.BoolVariation(string, bool)"/>
        public bool BoolVariation(string key, bool defaultValue = false)
        {
            return VariationInternal<bool>(key, defaultValue, ValueTypes.Bool, _eventFactoryDefault).Value;
        }

        /// <see cref="ILdClient.BoolVariationDetail(string, bool)"/>
        public EvaluationDetail<bool> BoolVariationDetail(string key, bool defaultValue = false)
        {
            return VariationInternal<bool>(key, defaultValue, ValueTypes.Bool, _eventFactoryWithReasons);
        }

        /// <see cref="ILdClient.StringVariation(string, string)"/>
        public string StringVariation(string key, string defaultValue)
        {
            return VariationInternal<string>(key, defaultValue, ValueTypes.String, _eventFactoryDefault).Value;
        }

        /// <see cref="ILdClient.StringVariationDetail(string, string)"/>
        public EvaluationDetail<string> StringVariationDetail(string key, string defaultValue)
        {
            return VariationInternal<string>(key, defaultValue, ValueTypes.String, _eventFactoryWithReasons);
        }

        /// <see cref="ILdClient.FloatVariation(string, float)"/>
        public float FloatVariation(string key, float defaultValue = 0)
        {
            return VariationInternal<float>(key, defaultValue, ValueTypes.Float, _eventFactoryDefault).Value;
        }

        /// <see cref="ILdClient.FloatVariationDetail(string, float)"/>
        public EvaluationDetail<float> FloatVariationDetail(string key, float defaultValue = 0)
        {
            return VariationInternal<float>(key, defaultValue, ValueTypes.Float, _eventFactoryWithReasons);
        }

        /// <see cref="ILdClient.IntVariation(string, int)"/>
        public int IntVariation(string key, int defaultValue = 0)
        {
            return VariationInternal(key, defaultValue, ValueTypes.Int, _eventFactoryDefault).Value;
        }

        /// <see cref="ILdClient.IntVariationDetail(string, int)"/>
        public EvaluationDetail<int> IntVariationDetail(string key, int defaultValue = 0)
        {
            return VariationInternal(key, defaultValue, ValueTypes.Int, _eventFactoryWithReasons);
        }

        /// <see cref="ILdClient.JsonVariation(string, ImmutableJsonValue)"/>
        public ImmutableJsonValue JsonVariation(string key, ImmutableJsonValue defaultValue)
        {
            return VariationInternal(key, defaultValue, ValueTypes.Json, _eventFactoryDefault).Value;
        }

        /// <see cref="ILdClient.JsonVariationDetail(string, ImmutableJsonValue)"/>
        public EvaluationDetail<ImmutableJsonValue> JsonVariationDetail(string key, ImmutableJsonValue defaultValue)
        {
            return VariationInternal(key, defaultValue, ValueTypes.Json, _eventFactoryWithReasons);
        }

        EvaluationDetail<T> VariationInternal<T>(string featureKey, T defaultValue, ValueType<T> desiredType, EventFactory eventFactory)
        {
            FeatureFlagEvent featureFlagEvent = FeatureFlagEvent.Default(featureKey);
            JToken defaultJson = desiredType.ValueToJson(defaultValue);

            EvaluationDetail<T> errorResult(EvaluationErrorKind kind) =>
                new EvaluationDetail<T>(defaultValue, null, new EvaluationReason.Error(kind));

            var flag = flagCacheManager.FlagForUser(featureKey, User);
            if (flag == null)
            {
                if (!Initialized)
                {
                    Log.Warn("LaunchDarkly client has not yet been initialized. Returning default value");
                    eventProcessor.SendEvent(eventFactory.NewUnknownFeatureRequestEvent(featureKey, User, defaultJson,
                        EvaluationErrorKind.CLIENT_NOT_READY));
                    return errorResult(EvaluationErrorKind.CLIENT_NOT_READY);
                }
                else
                {
                    Log.InfoFormat("Unknown feature flag {0}; returning default value", featureKey);
                    eventProcessor.SendEvent(eventFactory.NewUnknownFeatureRequestEvent(featureKey, User, defaultJson,
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

            featureFlagEvent = new FeatureFlagEvent(featureKey, flag);
            EvaluationDetail<T> result;
            JToken valueJson;
            if (flag.value == null || flag.value.Type == JTokenType.Null)
            {
                valueJson = defaultJson;
                result = new EvaluationDetail<T>(defaultValue, flag.variation, flag.reason);
            }
            else
            {
                try
                {
                    valueJson = flag.value;
                    var value = desiredType.ValueFromJson(flag.value);
                    result = new EvaluationDetail<T>(value, flag.variation, flag.reason);
                }
                catch (Exception)
                {
                    valueJson = defaultJson;
                    result = new EvaluationDetail<T>(defaultValue, null, new EvaluationReason.Error(EvaluationErrorKind.WRONG_TYPE));
                }
            }
            var featureEvent = eventFactory.NewFeatureRequestEvent(featureFlagEvent, User,
                new EvaluationDetail<JToken>(valueJson, flag.variation, flag.reason), defaultJson);
            eventProcessor.SendEvent(featureEvent);
            return result;
        }

        /// <see cref="ILdClient.AllFlags()"/>
        public IDictionary<string, ImmutableJsonValue> AllFlags()
        {
            return flagCacheManager.FlagsForUser(User)
                                    .ToDictionary(p => p.Key, p => ImmutableJsonValue.FromSafeValue(p.Value.value));
        }

        /// <see cref="ILdClient.Track(string, ImmutableJsonValue)"/>
        public void Track(string eventName, ImmutableJsonValue data)
        {
            eventProcessor.SendEvent(_eventFactoryDefault.NewCustomEvent(eventName, User, data.AsJToken()));
        }

        /// <see cref="ILdClient.Track(string)"/>
        public void Track(string eventName)
        {
            Track(eventName, ImmutableJsonValue.Null);
        }

        /// <see cref="ILdClient.Flush()"/>
        public void Flush()
        {
            eventProcessor.Flush();
        }

        /// <see cref="ILdClient.Identify(User, TimeSpan)"/>
        public bool Identify(User user, TimeSpan maxWaitTime)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return AsyncUtils.WaitSafely(() => IdentifyAsync(user), maxWaitTime);
        }

        /// <see cref="ILdClient.IdentifyAsync(User)"/>
        public async Task<bool> IdentifyAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            User newUser = DecorateUser(user);

            LockUtils.WithWriteLock(_userLock, () =>
            {
                _user = newUser;
            });

            eventProcessor.SendEvent(_eventFactoryDefault.NewIdentifyEvent(newUser));

            return await _connectionManager.SetUpdateProcessorFactory(
                Factory.CreateUpdateProcessorFactory(_config, user, flagCacheManager, _inBackground),
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

                BackgroundDetection.BackgroundModeChanged -= OnBackgroundModeChanged;
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
            Log.DebugFormat("Background mode is changing to {0}", args.IsInBackground);
            if (args.IsInBackground)
            {
                _inBackground = true;
                if (!Config.EnableBackgroundUpdating)
                {
                    Log.Debug("Background updating is disabled");
                    await _connectionManager.SetUpdateProcessorFactory(null, false);
                    return;
                }
                Log.Debug("Background updating is enabled, starting polling processor");
            }
            else
            {
                _inBackground = false;
            }
            await _connectionManager.SetUpdateProcessorFactory(
                Factory.CreateUpdateProcessorFactory(_config, User, flagCacheManager, _inBackground),
                false  // don't reset initialized state because the user is still the same
            );
        }
    }
}
