using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Common;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin
{
    /// <summary>
    /// A client for the LaunchDarkly API. Client instances are thread-safe. Your application should instantiate
    /// a single <c>LdClient</c> for the lifetime of their application.
    /// </summary>
    public sealed class LdClient : ILdMobileClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LdClient));

        /// <summary>
        /// The singleton instance used by your application throughout its lifetime, can only be created once.
        /// 
        /// Use the designated static method <see cref="Init(Configuration, User)"/> 
        /// to set this LdClient instance.
        /// </summary>
        /// <value>The LdClient instance.</value>
        public static LdClient Instance { get; internal set; }

        /// <summary>
        /// The Configuration instance used to setup the LdClient.
        /// </summary>
        /// <value>The Configuration instance.</value>
        public Configuration Config { get; private set; }

        /// <summary>
        /// The User for the LdClient operations.
        /// </summary>
        /// <value>The User.</value>
        public User User { get; private set; }

        object myLockObjForConnectionChange = new object();
        object myLockObjForUserUpdate = new object();

        IFlagCacheManager flagCacheManager;
        IConnectionManager connectionManager;
        IMobileUpdateProcessor updateProcessor;
        IEventProcessor eventProcessor;
        ISimplePersistance persister;
        IDeviceInfo deviceInfo;
        readonly EventFactory eventFactoryDefault = EventFactory.Default;
        readonly EventFactory eventFactoryWithReasons = EventFactory.DefaultWithReasons;
        IFeatureFlagListenerManager flagListenerManager;
        IPlatformAdapter platformAdapter;

        SemaphoreSlim connectionLock;

        // private constructor prevents initialization of this class
        // without using WithConfigAnduser(config, user)
        LdClient() { }

        LdClient(Configuration configuration, User user)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            Config = configuration;

            connectionLock = new SemaphoreSlim(1, 1);

            persister = Factory.CreatePersister(configuration);
            deviceInfo = Factory.CreateDeviceInfo(configuration);
            flagListenerManager = Factory.CreateFeatureFlagListenerManager(configuration);
            platformAdapter = new LaunchDarkly.Xamarin.BackgroundAdapter.BackgroundAdapter();

            // If you pass in a user with a null or blank key, one will be assigned to them.
            if (String.IsNullOrEmpty(user.Key))
            {
                User = UserWithUniqueKey(user);
            }
            else
            {
                User = user;
            }

            flagCacheManager = Factory.CreateFlagCacheManager(configuration, persister, flagListenerManager, User);
            connectionManager = Factory.CreateConnectionManager(configuration);
            updateProcessor = Factory.CreateUpdateProcessor(configuration, User, flagCacheManager, configuration.PollingInterval);
            eventProcessor = Factory.CreateEventProcessor(configuration);

            eventProcessor.SendEvent(eventFactoryDefault.NewIdentifyEvent(User));

            SetupConnectionManager();
        }

        /// <summary>
        /// Creates and returns new LdClient singleton instance, then starts the workflow for 
        /// fetching feature flags.
        /// 
        /// This constructor will wait and block on the current thread until initialization and the
        /// first response from the LaunchDarkly service is returned, up to the specified timeout.
        /// If you would rather this happen in an async fashion you can use <see cref="InitAsync(string, User)"/>.
        /// 
        /// This is the creation point for LdClient, you must use this static method or the more specific
        /// <see cref="Init(Configuration, User)"/> to instantiate the single instance of LdClient
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
        /// Creates and returns new LdClient singleton instance, then starts the workflow for 
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
        /// Creates and returns new LdClient singleton instance, then starts the workflow for 
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

            CreateInstance(config, user);

            if (Instance.Online)
            {
                if (!Instance.StartUpdateProcessor(maxWaitTime))
                {
                    Log.WarnFormat("Client did not successfully initialize within {0} milliseconds.",
                        maxWaitTime.TotalMilliseconds);
                }
            }

            return Instance;
        }

        /// <summary>
        /// Creates and returns new LdClient singleton instance, then starts the workflow for 
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
        public static Task<LdClient> InitAsync(Configuration config, User user)
        {
            CreateInstance(config, user);

            if (Instance.Online)
            {
                Task t = Instance.StartUpdateProcessorAsync();
                return t.ContinueWith((result) => Instance);
            }
            else
            {
                return Task.FromResult(Instance);
            }
        }

        static void CreateInstance(Configuration configuration, User user)
        {
            if (Instance != null)
            {
                throw new Exception("LdClient instance already exists.");
            }

            Instance = new LdClient(configuration, user);
            Log.InfoFormat("Initialized LaunchDarkly Client {0}",
                           Instance.Version);

            TimeSpan? bgPollInterval = null;
            if (configuration.EnableBackgroundUpdating)
            {
                bgPollInterval = configuration.BackgroundPollingInterval;
            }
            try
            {
                Instance.platformAdapter.EnableBackgrounding(new LdClientBackgroundingState(Instance));
            }
            catch
            {
                Log.Info("Foreground/Background is only available on iOS and Android");
            }
        }

        bool StartUpdateProcessor(TimeSpan maxWaitTime)
        {
            var initTask = updateProcessor.Start();
            try
            {
                return initTask.Wait(maxWaitTime);
            }
            catch (AggregateException e)
            {
                throw UnwrapAggregateException(e);
            }
        }

        Task StartUpdateProcessorAsync()
        {
            return updateProcessor.Start();
        }

        void SetupConnectionManager()
        {
            if (connectionManager is MobileConnectionManager mobileConnectionManager)
            {
                mobileConnectionManager.ConnectionChanged += MobileConnectionManager_ConnectionChanged;
                Log.InfoFormat("The mobile client connection changed online to {0}",
                               connectionManager.IsConnected);
            }
            online = connectionManager.IsConnected;
        }

        bool online;
        /// <see cref="ILdMobileClient.Online"/>
        public bool Online
        {
            get => online;
            set
            {
                var doNotAwaitResult = SetOnlineAsync(value);
            }
        }

        public async Task SetOnlineAsync(bool value)
        {
            await connectionLock.WaitAsync();
            online = value;
            try
            {
                if (online)
                {
                    await RestartUpdateProcessorAsync(Config.PollingInterval);
                }
                else
                {
                    ClearUpdateProcessor();
                }
            }
            finally
            {
                connectionLock.Release();
            }

            return;
        }

        void MobileConnectionManager_ConnectionChanged(bool isOnline)
        {
            Online = isOnline;
        }

        /// <see cref="ILdMobileClient.BoolVariation(string, bool)"/>
        public bool BoolVariation(string key, bool defaultValue = false)
        {
            return VariationInternal<bool>(key, defaultValue, ValueTypes.Bool, eventFactoryDefault).Value;
        }

        /// <see cref="ILdMobileClient.BoolVariationDetail(string, bool)"/>
        public EvaluationDetail<bool> BoolVariationDetail(string key, bool defaultValue = false)
        {
            return VariationInternal<bool>(key, defaultValue, ValueTypes.Bool, eventFactoryWithReasons);
        }

        /// <see cref="ILdMobileClient.StringVariation(string, string)"/>
        public string StringVariation(string key, string defaultValue)
        {
            return VariationInternal<string>(key, defaultValue, ValueTypes.String, eventFactoryDefault).Value;
        }

        /// <see cref="ILdMobileClient.StringVariationDetail(string, string)"/>
        public EvaluationDetail<string> StringVariationDetail(string key, string defaultValue)
        {
            return VariationInternal<string>(key, defaultValue, ValueTypes.String, eventFactoryWithReasons);
        }

        /// <see cref="ILdMobileClient.FloatVariation(string, float)"/>
        public float FloatVariation(string key, float defaultValue = 0)
        {
            return VariationInternal<float>(key, defaultValue, ValueTypes.Float, eventFactoryDefault).Value;
        }

        /// <see cref="ILdMobileClient.FloatVariationDetail(string, float)"/>
        public EvaluationDetail<float> FloatVariationDetail(string key, float defaultValue = 0)
        {
            return VariationInternal<float>(key, defaultValue, ValueTypes.Float, eventFactoryWithReasons);
        }

        /// <see cref="ILdMobileClient.IntVariation(string, int)"/>
        public int IntVariation(string key, int defaultValue = 0)
        {
            return VariationInternal(key, defaultValue, ValueTypes.Int, eventFactoryDefault).Value;
        }

        /// <see cref="ILdMobileClient.IntVariationDetail(string, int)"/>
        public EvaluationDetail<int> IntVariationDetail(string key, int defaultValue = 0)
        {
            return VariationInternal(key, defaultValue, ValueTypes.Int, eventFactoryWithReasons);
        }

        /// <see cref="ILdMobileClient.JsonVariation(string, JToken)"/>
        public JToken JsonVariation(string key, JToken defaultValue)
        {
            return VariationInternal(key, defaultValue, ValueTypes.Json, eventFactoryDefault).Value;
        }

        /// <see cref="ILdMobileClient.JsonVariationDetail(string, JToken)"/>
        public EvaluationDetail<JToken> JsonVariationDetail(string key, JToken defaultValue)
        {
            return VariationInternal(key, defaultValue, ValueTypes.Json, eventFactoryWithReasons);
        }

        EvaluationDetail<T> VariationInternal<T>(string featureKey, T defaultValue, ValueType<T> desiredType, EventFactory eventFactory)
        {
            FeatureFlagEvent featureFlagEvent = FeatureFlagEvent.Default(featureKey);
            JToken defaultJson = desiredType.ValueToJson(defaultValue);

            EvaluationDetail<T> errorResult(EvaluationErrorKind kind) =>
                new EvaluationDetail<T>(defaultValue, null, new EvaluationReason.Error(kind));

            if (!Initialized())
            {
                Log.Warn("LaunchDarkly client has not yet been initialized. Returning default");
                return errorResult(EvaluationErrorKind.CLIENT_NOT_READY);
            }

            var flag = flagCacheManager.FlagForUser(featureKey, User);
            if (flag == null)
            {
                Log.InfoFormat("Unknown feature flag {0}; returning default value", featureKey);
                eventProcessor.SendEvent(eventFactory.NewUnknownFeatureRequestEvent(featureKey, User, defaultJson,
                    EvaluationErrorKind.FLAG_NOT_FOUND));
                return errorResult(EvaluationErrorKind.FLAG_NOT_FOUND);
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

        /// <see cref="ILdMobileClient.AllFlags()"/>
        public IDictionary<string, JToken> AllFlags()
        {
            if (IsOffline())
            {
                Log.Warn("AllFlags() was called when client is in offline mode. Returning null.");
                return null;
            }
            if (!Initialized())
            {
                Log.Warn("AllFlags() was called before client has finished initializing. Returning null.");
                return null;
            }

            return flagCacheManager.FlagsForUser(User)
                                    .ToDictionary(p => p.Key, p => p.Value.value);
        }

        /// <see cref="ILdMobileClient.Track(string, JToken)"/>
        public void Track(string eventName, JToken data)
        {
            eventProcessor.SendEvent(eventFactoryDefault.NewCustomEvent(eventName, User, data));
        }

        /// <see cref="ILdMobileClient.Track(string)"/>
        public void Track(string eventName)
        {
            Track(eventName, null);
        }

        /// <see cref="ILdMobileClient.Initialized"/>
        public bool Initialized()
        {
            return Online && updateProcessor.Initialized();
        }

        /// <see cref="ILdCommonClient.IsOffline()"/>
        public bool IsOffline()
        {
            return !online;
        }

        /// <see cref="ILdCommonClient.Flush()"/>
        public void Flush()
        {
            eventProcessor.Flush();
        }

        /// <see cref="ILdMobileClient.Identify(User)"/>
        public void Identify(User user)
        {
            try
            {
                // Note that we must use Task.Run here, rather than just doing IdentifyAsync(user).Wait(),
                // to avoid a deadlock if we are on the main thread. See:
                // https://olitee.com/2015/01/c-async-await-common-deadlock-scenario/
                Task.Run(() => IdentifyAsync(user)).Wait();
            }
            catch (AggregateException e)
            {
                throw UnwrapAggregateException(e);
            }
        }

        /// <see cref="ILdMobileClient.IdentifyAsync(User)"/>
        public async Task IdentifyAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            User userWithKey = user;
            if (String.IsNullOrEmpty(user.Key))
            {
                userWithKey = UserWithUniqueKey(user);
            }

            await connectionLock.WaitAsync();
            try
            {
                User = userWithKey;
                await RestartUpdateProcessorAsync(Config.PollingInterval);
            }
            finally
            {
                connectionLock.Release();
            }

            eventProcessor.SendEvent(eventFactoryDefault.NewIdentifyEvent(userWithKey));
        }

        async Task RestartUpdateProcessorAsync(TimeSpan pollingInterval)
        {
            ClearAndSetUpdateProcessor(pollingInterval);
            await StartUpdateProcessorAsync();
        }

        void ClearAndSetUpdateProcessor(TimeSpan pollingInterval)
        {
            ClearUpdateProcessor();
            updateProcessor = Factory.CreateUpdateProcessor(Config, User, flagCacheManager, pollingInterval);
        }

        void ClearUpdateProcessor()
        {
            if (updateProcessor != null)
            {
                updateProcessor.Dispose();
                updateProcessor = new NullUpdateProcessor();
            }
        }

        User UserWithUniqueKey(User user)
        {
            string uniqueId = deviceInfo.UniqueDeviceId();
            return new User(user)
            {
                Key = uniqueId,
                Anonymous = true
            };
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                Log.InfoFormat("Shutting down the LaunchDarkly client");

                try
                {
                    platformAdapter.Dispose();
                }
                catch(Exception error)
                {
                    Log.Error(error);
                }
                updateProcessor.Dispose();
                eventProcessor.Dispose();
            }
        }

        /// <see cref="ILdCommonClient.Version"/>
        public Version Version
        {
            get
            {
                return MobileClientEnvironment.Instance.Version;
            }
        }

        /// <see cref="ILdMobileClient.RegisterFeatureFlagListener(string, IFeatureFlagListener)"/>
        public void RegisterFeatureFlagListener(string flagKey, IFeatureFlagListener listener)
        {
            flagListenerManager.RegisterListener(listener, flagKey);
        }

        /// <see cref="ILdMobileClient.UnregisterFeatureFlagListener(string, IFeatureFlagListener)"/>
        public void UnregisterFeatureFlagListener(string flagKey, IFeatureFlagListener listener)
        {
            flagListenerManager.UnregisterListener(listener, flagKey);
        }

        internal async Task EnterBackgroundAsync()
        {
            ClearUpdateProcessor();
            Config.IsStreamingEnabled = false;
            if (Config.EnableBackgroundUpdating)
            {
                await RestartUpdateProcessorAsync(Config.BackgroundPollingInterval);
            }
            persister.Save(Constants.BACKGROUNDED_WHILE_STREAMING, "true");
        }

        internal async Task EnterForegroundAsync()
        {
            ResetProcessorForForeground();
            await RestartUpdateProcessorAsync(Config.PollingInterval);
        }

        void ResetProcessorForForeground()
        {
            string didBackground = persister.GetValue(Constants.BACKGROUNDED_WHILE_STREAMING);
            if (didBackground.Equals("true"))
            {
                persister.Save(Constants.BACKGROUNDED_WHILE_STREAMING, "false");
                ClearUpdateProcessor();
                Config.IsStreamingEnabled = true;
            }
        }

        private Exception UnwrapAggregateException(AggregateException e)
        {
            if (e.InnerExceptions.Count == 1)
            {
                return e.InnerExceptions[0];
            }
            return e;
        }
    }

    // Implementation of IBackgroundingState - this allows us to keep these methods out of the public LdClient API
    internal class LdClientBackgroundingState : IBackgroundingState
    {
        private readonly LdClient _client;

        internal LdClientBackgroundingState(LdClient client)
        {
            _client = client;
        }

        public async Task EnterBackgroundAsync()
        {
            await _client.EnterBackgroundAsync();
        }

        public async Task ExitBackgroundAsync()
        {
            await _client.EnterForegroundAsync();
        }
    }
}