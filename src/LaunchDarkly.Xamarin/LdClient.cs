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

        SemaphoreSlim connectionLock;

        // private constructor prevents initialization of this class
        // without using WithConfigAnduser(config, user)
        LdClient() { }

        LdClient(Configuration configuration, User user)
        {
            Config = configuration;

            connectionLock = new SemaphoreSlim(1, 1);

            persister = Factory.CreatePersister(configuration);
            deviceInfo = Factory.CreateDeviceInfo(configuration);
            flagListenerManager = Factory.CreateFeatureFlagListenerManager(configuration);

            // If you pass in a null user or user with a null key, one will be assigned to them.
            if (user == null || user.Key == null)
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

            SetupConnectionManager();
        }

        /// <summary>
        /// Creates and returns new LdClient singleton instance, then starts the workflow for 
        /// fetching feature flags.
        /// 
        /// This constructor will wait and block on the current thread until initialization and the
        /// first response from the LaunchDarkly service is returned, if you would rather this happen
        /// in an async fashion you can use <see cref="InitAsync(string, User)"/>
        /// 
        /// This is the creation point for LdClient, you must use this static method or the more specific
        /// <see cref="Init(Configuration, User)"/> to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </summary>
        /// <returns>The singleton LdClient instance.</returns>
        /// <param name="mobileKey">The mobile key given to you by LaunchDarkly.</param>
        /// <param name="user">The user needed for client operations.</param>
        public static LdClient Init(string mobileKey, User user)
        {
            var config = Configuration.Default(mobileKey);

            return Init(config, user);
        }

        /// <summary>
        /// Creates and returns new LdClient singleton instance, then starts the workflow for 
        /// fetching feature flags. This constructor should be used if you do not want to wait 
        /// for the IUpdateProcessor instance to finish initializing and receive the first response
        /// from the LaunchDarkly service.
        /// 
        /// This is the creation point for LdClient, you must use this static method or the more specific
        /// <see cref="Init(Configuration, User)"/> to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </summary>
        /// <returns>The singleton LdClient instance.</returns>
        /// <param name="mobileKey">The mobile key given to you by LaunchDarkly.</param>
        /// <param name="user">The user needed for client operations.</param>
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
        /// first response from the LaunchDarkly service is returned, if you would rather this happen
        /// in an async fashion you can use <see cref="InitAsync(Configuration, User)"/>
        /// 
        /// This is the creation point for LdClient, you must use this static method or the more basic
        /// <see cref="Init(string, User)"/> to instantiate the single instance of LdClient
        /// for the lifetime of your application.
        /// </summary>
        /// <returns>The singleton LdClient instance.</returns>
        /// <param name="config">The client configuration object</param>
        /// <param name="user">The user needed for client operations.</param>
        public static LdClient Init(Configuration config, User user)
        {
            CreateInstance(config, user);

            if (Instance.Online)
            {
                StartUpdateProcessor();
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
        /// <param name="user">The user needed for client operations.</param>
        public static Task<LdClient> InitAsync(Configuration config, User user)
        {
            CreateInstance(config, user);

            if (Instance.Online)
            {
                Task t = StartUpdateProcessorAsync();
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
                throw new Exception("LdClient instance already exists.");

            Instance = new LdClient(configuration, user);
            Log.InfoFormat("Initialized LaunchDarkly Client {0}",
                           Instance.Version);
        }

        static void StartUpdateProcessor()
        {
            var initTask = Instance.updateProcessor.Start();
            var configuration = Instance.Config as Configuration;
            var unused = initTask.Wait(configuration.StartWaitTime);
        }

        static Task StartUpdateProcessorAsync()
        {
            return Instance.updateProcessor.Start();
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
            get
            {
                return online;
            }
            set
            {
                SetOnlineAsync(value).Wait();
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
                                returnedFlagValue.GetType(),
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

            if (User == null || User.Key == null)
            {
                Log.Warn("Feature flag evaluation called with null user or null user key. Returning default");
                featureRequestEvent = eventFactory.NewDefaultFeatureRequestEvent(featureFlagEvent,
                                                                                 User,
                                                                                 defaultValue);
                eventProcessor.SendEvent(featureRequestEvent);
                return defaultValue;
            }

            var flag = flagCacheManager.FlagForUser(featureKey, User);
            if (flag != null)
            {
                featureFlagEvent = new FeatureFlagEvent(featureKey, flag);
                featureRequestEvent = eventFactory.NewFeatureRequestEvent(featureFlagEvent,
                                                                          User,
                                                                          flag.variation,
                                                                          flag.value,
                                                                          defaultValue);
                eventProcessor.SendEvent(featureRequestEvent);
                return flag.value;
            }

            Log.InfoFormat("Unknown feature flag {0}; returning default value",
                        featureKey);
            featureRequestEvent = eventFactory.NewUnknownFeatureRequestEvent(featureKey,
                                                                             User,
                                                                             defaultValue);
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
            if (User == null || User.Key == null)
            {
                Log.Warn("AllFlags() called with null user or null user key. Returning null");
                return null;
            }

            return flagCacheManager.FlagsForUser(User)
                                    .ToDictionary(p => p.Key, p => p.Value.value);
        }

        /// <see cref="ILdMobileClient.Track(string, JToken)"/>
        public void Track(string eventName, JToken data)
        {
            if (User == null || User.Key == null)
            {
                Log.Warn("Track called with null user or null user key");
            }

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
            bool isInited = Instance != null;
            return isInited && Online;
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
            IdentifyAsync(user).Wait();
        }

        /// <see cref="ILdMobileClient.IdentifyAsync(User)"/>
        public async Task IdentifyAsync(User user)
        {
            if (user == null)
            {
                Log.Warn("Identify called with null user");
                return;
            }

            User userWithKey = null;
            if (user.Key == null)
            {
                userWithKey = UserWithUniqueKey(user);
            }
            else
            {
                userWithKey = user;
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

        void RestartUpdateProcessor()
        {
            ClearAndSetUpdateProcessor();
            StartUpdateProcessor();
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
                updateProcessor = null;
            }
        }

        User UserWithUniqueKey(User user = null)
        {
            string uniqueId = deviceInfo.UniqueDeviceId();

            if (user != null)
            {
                var updatedUser = new User(user)
                {
                    Key = uniqueId,
                    Anonymous = true
                };

                return updatedUser;
            }

            return new User(uniqueId);
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
                Log.InfoFormat("The mobile client is being disposed");
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

        internal void EnterBackground()
        {
            // if using Streaming, processor needs to be reset
            if (Config.IsStreamingEnabled)
            {
                ClearUpdateProcessor();
                Config.IsStreamingEnabled = false;
                RestartUpdateProcessor();
                persister.Save(Constants.BACKGROUNDED_WHILE_STREAMING, "true");
            }
            else
            {
                PingPollingProcessor();
            }
        }

        internal async Task EnterBackgroundAsync()
        {
            // if using Streaming, processor needs to be reset
            if (Config.IsStreamingEnabled)
            {
                ClearUpdateProcessor();
                Config.IsStreamingEnabled = false;
                await RestartUpdateProcessorAsync();
                persister.Save(Constants.BACKGROUNDED_WHILE_STREAMING, "true");
            }
            else
            {
                await PingPollingProcessorAsync();
            }
        }

        internal void EnterForeground()
        {
            ResetProcessorForForeground();
            RestartUpdateProcessor();
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
                var waitTask = pollingProcessor.PingAndWait();
                waitTask.Wait();
            }
        }

        async Task PingPollingProcessorAsync()
        {
            var pollingProcessor = updateProcessor as MobilePollingProcessor;
            if (pollingProcessor != null)
            {
                await pollingProcessor.PingAndWait();
            }
        }
    }
}