using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal.DataSources;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal static class Factory
    {
        internal static IFlagCacheManager CreateFlagCacheManager(Configuration configuration, 
                                                                 IPersistentStorage persister,
                                                                 IFlagChangedEventManager flagChangedEventManager,
                                                                 User user,
                                                                 Logger log)
        {
            if (configuration.FlagCacheManager != null)
            {
                return configuration.FlagCacheManager;
            }
            else
            {
                var inMemoryCache = new UserFlagInMemoryCache();
                var deviceCache = configuration.PersistFlagValues ? new UserFlagDeviceCache(persister, log) as IUserFlagCache : new NullUserFlagCache();
                return new FlagCacheManager(inMemoryCache, deviceCache, flagChangedEventManager, user);
            }
        }

        internal static IConnectivityStateManager CreateConnectivityStateManager(Configuration configuration)
        {
            return configuration.ConnectivityStateManager ?? new DefaultConnectivityStateManager();
        }

        internal static IPersistentStorage CreatePersistentStorage(Configuration configuration, Logger log)
        {
            return configuration.PersistentStorage ?? new DefaultPersistentStorage(log);
        }

        internal static IDeviceInfo CreateDeviceInfo(Configuration configuration, Logger log)
        {
            return configuration.DeviceInfo ?? new DefaultDeviceInfo(log);
        }

        internal static IFlagChangedEventManager CreateFlagChangedEventManager(Configuration configuration, Logger log)
        {
            return configuration.FlagChangedEventManager ?? new FlagChangedEventManager(log);
        }
    }
}
