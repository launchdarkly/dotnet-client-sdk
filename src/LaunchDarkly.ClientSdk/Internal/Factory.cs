using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal.DataSources;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Internal.Events;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Events;

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

        internal static IEventProcessor CreateEventProcessor(Configuration configuration, Logger baseLog)
        {
            if (configuration.EventProcessor != null)
            {
                return configuration.EventProcessor;
            }

            var log = baseLog.SubLogger(LogNames.EventsSubLog);
            var eventsConfig = new EventsConfiguration
            {
                AllAttributesPrivate = configuration.AllAttributesPrivate,
                DiagnosticRecordingInterval = TimeSpan.FromMinutes(15), // TODO
                DiagnosticUri = null,
                EventCapacity = configuration.EventCapacity,
                EventFlushInterval = configuration.EventFlushInterval,
                EventsUri = configuration.EventsUri.AddPath(Constants.EVENTS_PATH),
                InlineUsersInEvents = configuration.InlineUsersInEvents,
                PrivateAttributeNames = configuration.PrivateAttributeNames,
                RetryInterval = TimeSpan.FromSeconds(1)
            };
            var httpProperties = configuration.HttpProperties;
            var eventProcessor = new EventProcessor(
                eventsConfig,
                new DefaultEventSender(httpProperties, eventsConfig, log),
                null,
                null,
                null,
                log,
                null
                );
            return new DefaultEventProcessorWrapper(eventProcessor);
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
