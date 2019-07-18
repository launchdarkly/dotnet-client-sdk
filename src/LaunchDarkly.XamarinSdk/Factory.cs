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
            if (configuration.FlagCacheManager != null)
            {
                return configuration.FlagCacheManager;
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
            return configuration.ConnectionManager ?? new MobileConnectionManager();
        }

        internal static IMobileUpdateProcessor CreateUpdateProcessor(Configuration configuration, User user, IFlagCacheManager flagCacheManager, TimeSpan? overridePollingInterval)
        {
            if (configuration.Offline)
            {
                Log.InfoFormat("Starting LaunchDarkly client in offline mode");
                return new NullUpdateProcessor();
            }

            if (configuration.UpdateProcessorFactory != null)
            {
                return configuration.UpdateProcessorFactory(configuration, flagCacheManager, user);
            }

            if (configuration.IsStreamingEnabled)
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

        internal static IPersistentStorage CreatePersistentStorage(Configuration configuration)
        {
            return configuration.PersistentStorage ?? new DefaultPersistentStorage();
        }

        internal static IDeviceInfo CreateDeviceInfo(Configuration configuration)
        {
            return configuration.DeviceInfo ?? new DefaultDeviceInfo();
        }

        internal static IFlagChangedEventManager CreateFlagChangedEventManager(Configuration configuration)
        {
            return configuration.FlagChangedEventManager ?? new FlagChangedEventManager();
        }
    }
}
