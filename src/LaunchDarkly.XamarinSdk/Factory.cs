using System;
using System.Net.Http;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Common;

namespace LaunchDarkly.Xamarin
{
    internal static class Factory
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Factory));

        internal static IFlagCacheManager CreateFlagCacheManager(Configuration configuration, 
                                                                 IPersistentStorage persister,
                                                                 IFlagChangedEventManager flagChangedEventManager,
                                                                 User user)
        {
            if (configuration._flagCacheManager != null)
            {
                return configuration._flagCacheManager;
            }
            else
            {
                var inMemoryCache = new UserFlagInMemoryCache();
                var deviceCache = configuration.PersistFlagValues ? new UserFlagDeviceCache(persister) as IUserFlagCache : new NullUserFlagCache();
                return new FlagCacheManager(inMemoryCache, deviceCache, flagChangedEventManager, user);
            }
        }

        internal static IConnectionManager CreateConnectionManager(Configuration configuration)
        {
            return configuration._connectionManager ?? new MobileConnectionManager();
        }

        internal static IMobileUpdateProcessor CreateUpdateProcessor(Configuration configuration, User user,
            IFlagCacheManager flagCacheManager, TimeSpan? overridePollingInterval,
            bool disableStreaming)
        {
            if (configuration.Offline)
            {
                Log.InfoFormat("Starting LaunchDarkly client in offline mode");
                return new NullUpdateProcessor();
            }

            if (configuration._updateProcessorFactory != null)
            {
                return configuration._updateProcessorFactory(configuration, flagCacheManager, user);
            }

            if (configuration.IsStreamingEnabled && !disableStreaming)
            {
                return new MobileStreamingProcessor(configuration, flagCacheManager, user, null);
            }
            else
            {
                var featureFlagRequestor = new FeatureFlagRequestor(configuration, user);
                return new MobilePollingProcessor(featureFlagRequestor,
                                                  flagCacheManager,
                                                  user,
                                                  overridePollingInterval ?? configuration.PollingInterval);
            }
        }

        internal static IEventProcessor CreateEventProcessor(Configuration configuration)
        {
            if (configuration._eventProcessor != null)
            {
                return configuration._eventProcessor;
            }
            if (configuration.Offline)
            {
                return new NullEventProcessor();
            }

            HttpClient httpClient = Util.MakeHttpClient(configuration, MobileClientEnvironment.Instance);
            return new DefaultEventProcessor(configuration, null, httpClient, Constants.EVENTS_PATH);
        }

        internal static IPersistentStorage CreatePersistentStorage(Configuration configuration)
        {
            return configuration._persistentStorage ?? new DefaultPersistentStorage();
        }

        internal static IDeviceInfo CreateDeviceInfo(Configuration configuration)
        {
            return configuration._deviceInfo ?? new DefaultDeviceInfo();
        }

        internal static IFlagChangedEventManager CreateFlagChangedEventManager(Configuration configuration)
        {
            return configuration._flagChangedEventManager ?? new FlagChangedEventManager();
        }
    }
}
