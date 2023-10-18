using LaunchDarkly.Sdk.EnvReporting.LayerModels;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class DeviceInfo
    {
        private static OsInfo? PlatformGetOsInfo() => null;

        private static LaunchDarkly.Sdk.EnvReporting.LayerModels.DeviceInfo? PlatformGetDeviceInfo() => null;
    }
}
