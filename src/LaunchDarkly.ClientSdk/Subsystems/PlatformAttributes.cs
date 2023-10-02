using LaunchDarkly.Sdk.Client.PlatformSpecific;
using LaunchDarkly.Sdk.EnvReporting;

namespace LaunchDarkly.Sdk.Client.Subsystems
{
    internal static class PlatformAttributes
    {
        internal static Layer Layer => new Layer(
            AppInfo.GetAppInfo(),
            DeviceInfo.GetOsInfo(),
            DeviceInfo.GetDeviceInfo()
        );
    }
}
