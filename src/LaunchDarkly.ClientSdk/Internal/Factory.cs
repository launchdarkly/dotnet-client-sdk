using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal.DataSources;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal static class Factory
    {
        internal static IConnectivityStateManager CreateConnectivityStateManager(Configuration configuration)
        {
            return configuration.ConnectivityStateManager ?? new DefaultConnectivityStateManager();
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
