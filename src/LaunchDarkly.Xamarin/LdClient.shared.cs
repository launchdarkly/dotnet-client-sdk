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
        EventFactory eventFactory = EventFactory.Default;
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

            configuration.PlatformAdapter = new LaunchDarkly.Xamarin.BackgroundAdapter.BackgroundAdapter();

            Config = configuration;

            connectionLock = new SemaphoreSlim(1, 1);

            persister = Factory.CreatePersister(configuration);
            deviceInfo = Factory.CreateDeviceInfo(configuration);
            flagListenerManager = Factory.CreateFeatureFlagListenerManager(configuration);
            platformAdapter = Factory.CreatePlatformAdapter(configuration);

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
            updateProcessor = Factory.CreateUpdateProcessor(configuration, User, flagCacheManager);
            eventProcessor = Factory.CreateEventProcessor(configuration);

            eventProcessor.SendEvent(eventFactory.NewIdentifyEvent(User));

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
                    await RestartUpdateProcessorAsync();
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
            return VariationWithType(key, defaultValue, JTokenType.Boolean).Value<bool>();
        }

        /// <see cref="ILdMobileClient.StringVariation(string, string)"/>
        public string StringVariation(string key, string defaultValue)
        {
            var value = VariationWithType(key, defaultValue, JTokenType.String);
            if (value != null)
            {
                return value.Value<string>();
            }

            return null;
        }

        /// <see cref="ILdMobileClient.FloatVariation(string, float)"/>
        public float FloatVariation(string key, float defaultValue = 0)
        {
            return VariationWithType(key, defaultValue, JTokenType.Float).Value<float>();
        }

        /// <see cref="ILdMobileClient.IntVariation(string, int)"/>
        public int IntVariation(string key, int defaultValue = 0)
        {
            return VariationWithType(key, defaultValue, JTokenType.Integer).Value<int>();
        }

        /// <see cref="ILdMobileClient.JsonVariation(string, JToken)"/>
        public JToken JsonVariation(string key, JToken defaultValue)
        {
            return VariationWithType(key, defaultValue, null);
        }

        JToken VariationWithType(string featureKey, JToken defaultValue, JTokenType? jtokenType)
        {
            var returnedFlagValue = Variation(featureKey, defaultValue);
            if (returnedFlagValue != null && jtokenType != null && !returnedFlagValue.Type.Equals(jtokenType))
            {
                Log.ErrorFormat("Expected type: {0} but got {1} when evaluating FeatureFlag: {2}. Returning default",
                                jtokenType,
                                returnedFlagValue.Type,
                                featureKey);
                
                return defaultValue;
            }

            return returnedFlagValue;
        }

        JToken Variation(string featureKey, JToken defaultValue)
        {
            FeatureFlagEvent featureFlagEvent = FeatureFlagEvent.Default(featureKey);
            FeatureRequestEvent featureRequestEvent;

            if (!Initialized())
            {
                Log.Warn("LaunchDarkly client has not yet been initialized. Returning default");
                return defaultValue;
            }
            
            var flag = flagCacheManager.FlagForUser(featureKey, User);
            if (flag != null)
            {
                featureFlagEvent = new FeatureFlagEvent(featureKey, flag);
                var value = flag.value;
                if (value == null || value.Type == JTokenType.Null) {
                    featureRequestEvent = eventFactory.NewDefaultFeatureRequestEvent(featureFlagEvent,
                                                                                     User,
                                                                                     defaultValue,
                                                                                     EvaluationErrorKind.FLAG_NOT_FOUND);
                    value = defaultValue;
                } else {
                    featureRequestEvent = eventFactory.NewFeatureRequestEvent(featureFlagEvent,
                                                                              User,
                                                                              new EvaluationDetail<JToken>(flag.value, flag.variation, null),
                                                                              defaultValue);
                }
                eventProcessor.SendEvent(featureRequestEvent);
                return value;
            }

            Log.InfoFormat("Unknown feature flag {0}; returning default value",
                        featureKey);
            featureRequestEvent = eventFactory.NewUnknownFeatureRequestEvent(featureKey,
                                                                             User,
                                                                             defaultValue,
                                                                             EvaluationErrorKind.FLAG_NOT_FOUND);
            eventProcessor.SendEvent(featureRequestEvent);
            return defaultValue;
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
            eventProcessor.SendEvent(eventFactory.NewCustomEvent(eventName, User, data));
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
                await RestartUpdateProcessorAsync();
            }
            finally
            {
                connectionLock.Release();
            }

            eventProcessor.SendEvent(eventFactory.NewIdentifyEvent(userWithKey));
        }

        async Task RestartUpdateProcessorAsync()
        {
            ClearAndSetUpdateProcessor();
            await StartUpdateProcessorAsync();
        }

        void ClearAndSetUpdateProcessor()
        {
            ClearUpdateProcessor();
            updateProcessor = Factory.CreateUpdateProcessor(Config, User, flagCacheManager);
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
                catch
                {
                    Log.Info("Foreground/Background is only available on iOS and Android");
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
            // if using Streaming, processor needs to be reset
            if (Config.IsStreamingEnabled)
            {
                ClearUpdateProcessor();
                Config.IsStreamingEnabled = false;
                if (Config.EnableBackgroundUpdating)
                {
                    await RestartUpdateProcessorAsync();
                    await PingPollingProcessorAsync();
                }
                persister.Save(Constants.BACKGROUNDED_WHILE_STREAMING, "true");
            }
            else
            {
                if (Config.EnableBackgroundUpdating)
                {
                    await PingPollingProcessorAsync();
                }
            }
        }

        internal async Task EnterForegroundAsync()
        {
            ResetProcessorForForeground();
            await RestartUpdateProcessorAsync();
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

        internal void BackgroundTick()
        {
            PingPollingProcessor();
        }

        internal async Task BackgroundTickAsync()
        {
            await PingPollingProcessorAsync();
        }

        void PingPollingProcessor()
        {
            var pollingProcessor = updateProcessor as MobilePollingProcessor;
            if (pollingProcessor != null)
            {
                var waitTask = pollingProcessor.PingAndWait(Config.BackgroundPollingInterval);
                waitTask.Wait();
            }
        }

        async Task PingPollingProcessorAsync()
        {
            var pollingProcessor = updateProcessor as MobilePollingProcessor;
            if (pollingProcessor != null)
            {
                await pollingProcessor.PingAndWait(Config.BackgroundPollingInterval);
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

        public async Task BackgroundUpdateAsync()
        {
            await _client.BackgroundTickAsync();
        }
    }
}