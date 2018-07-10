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
            IFlagCacheManager flagCacheManager;

            if (configuration.FlagCacheManager != null)
            {
                flagCacheManager = configuration.FlagCacheManager;
            }
            else
            {
                var inMemoryCache = new UserFlagInMemoryCache();
                var deviceCache = new UserFlagDeviceCache(persister);
                flagCacheManager = new FlagCacheManager(inMemoryCache, deviceCache, updater, user);
            }

            return flagCacheManager;
        }

        internal static IConnectionManager CreateConnectionManager(Configuration configuration)
        {
            IConnectionManager connectionManager;
            connectionManager = configuration.ConnectionManager ?? new MobileConnectionManager();
            return connectionManager;
        }

        internal static IMobileUpdateProcessor CreateUpdateProcessor(Configuration configuration,
                                                                     User user,
                                                                     IFlagCacheManager flagCacheManager,
                                                                     StreamManager.EventSourceCreator source = null)
        {
            if (configuration.MobileUpdateProcessor != null)
            {
                return configuration.MobileUpdateProcessor;
            }

            IMobileUpdateProcessor updateProcessor = null;
            if (configuration.Offline)
            {
                Log.InfoFormat("Was configured to be offline, starting service with NullUpdateProcessor");
                return new NullUpdateProcessor();
            }

            if (configuration.IsStreamingEnabled)
            {
                updateProcessor = new MobileStreamingProcessor(configuration,
                                                               flagCacheManager,
                                                               user, source);
            }
            else
            {
                var featureFlagRequestor = new FeatureFlagRequestor(configuration, user);
                updateProcessor = new MobilePollingProcessor(featureFlagRequestor,
                                                             flagCacheManager,
                                                             user,
                                                             configuration.PollingInterval);
            }

            return updateProcessor;
        }

        internal static IEventProcessor CreateEventProcessor(IBaseConfiguration configuration)
        {
            if (configuration.Offline)
            {
                Log.InfoFormat("Was configured to be offline, starting service with NullEventProcessor");
                return new NullEventProcessor();
            }

            HttpClient httpClient = Util.MakeHttpClient(configuration, MobileClientEnvironment.Instance);
            return new DefaultEventProcessor(configuration, null, httpClient, Constants.EVENTS_PATH);
        }

        internal static ISimplePersistance CreatePersister(Configuration configuration)
        {
            if (configuration.UseInMemoryPersistanceOnly)
            {
                return new SimpleInMemoryPersistance();
            }

            if (configuration.Persister != null)
            {
                return configuration.Persister;
            }

            return new SimpleMobileDevicePersistance();
        }

        internal static IDeviceInfo CreateDeviceInfo(Configuration configuration)
        {
            if (configuration.DeviceInfo != null)
            {
                return configuration.DeviceInfo;
            }

            return new DeviceInfo();
        }

        internal static IFeatureFlagListenerManager CreateFeatureFlagListenerManager(Configuration configuration)
        {
            if (configuration.FeatureFlagListenerManager != null)
            {
                return configuration.FeatureFlagListenerManager;
            }

            return new FeatureFlagListenerManager();
        }
    }
}
