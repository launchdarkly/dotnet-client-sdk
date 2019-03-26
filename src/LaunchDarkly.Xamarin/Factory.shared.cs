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
                                                                 ISimplePersistance persister,
                                                                 IFlagListenerUpdater updater,
                                                                 User user)
        {
            if (configuration.FlagCacheManager != null)
            {
                return configuration.FlagCacheManager;
            }
            else
            {
                var inMemoryCache = new UserFlagInMemoryCache();
                var deviceCache = new UserFlagDeviceCache(persister);
                return new FlagCacheManager(inMemoryCache, deviceCache, updater, user);
            }
        }

        internal static IConnectionManager CreateConnectionManager(Configuration configuration)
        {
            return configuration.ConnectionManager ?? new MobileConnectionManager();
        }

        internal static IMobileUpdateProcessor CreateUpdateProcessor(Configuration configuration,
                                                                     User user,
                                                                     IFlagCacheManager flagCacheManager,
                                                                     TimeSpan pollingInterval,
                                                                     StreamManager.EventSourceCreator source = null)
        {
            if (configuration.MobileUpdateProcessor != null)
            {
                return configuration.MobileUpdateProcessor;
            }

            if (configuration.Offline)
            {
                Log.InfoFormat("Starting LaunchDarkly client in offline mode");
                return new NullUpdateProcessor();
            }

            if (configuration.IsStreamingEnabled)
            {
                return new MobileStreamingProcessor(configuration,
                                                               flagCacheManager,
                                                               user, source);
            }
            else
            {
                var featureFlagRequestor = new FeatureFlagRequestor(configuration, user);
                return new MobilePollingProcessor(featureFlagRequestor,
                                                  flagCacheManager,
                                                  user,
                                                  pollingInterval);
            }
        }

        internal static IEventProcessor CreateEventProcessor(Configuration configuration)
        {
            if (configuration.EventProcessor != null)
            {
                return configuration.EventProcessor;
            }
            if (configuration.Offline)
            {
                return new NullEventProcessor();
            }

            HttpClient httpClient = Util.MakeHttpClient(configuration, MobileClientEnvironment.Instance);
            return new DefaultEventProcessor(configuration, null, httpClient, Constants.EVENTS_PATH);
        }

        internal static ISimplePersistance CreatePersister(Configuration configuration)
        {
            return configuration.Persister ?? new SimpleMobileDevicePersistance();
        }

        internal static IDeviceInfo CreateDeviceInfo(Configuration configuration)
        {
            return configuration.DeviceInfo ?? new DeviceInfo();
        }

        internal static IFeatureFlagListenerManager CreateFeatureFlagListenerManager(Configuration configuration)
        {
            return configuration.FeatureFlagListenerManager ?? new FeatureFlagListenerManager();
        }
    }
}
