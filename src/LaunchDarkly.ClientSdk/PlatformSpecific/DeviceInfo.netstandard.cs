using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.EnvReporting.LayerModels;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class DeviceInfo
    {
        private static IProp<OsInfo> PlatformGetOsInfo() => new Props.Fallthrough<OsInfo>();

        private static IProp<LaunchDarkly.Sdk.EnvReporting.LayerModels.DeviceInfo> PlatformGetDeviceInfo() =>
            new Props.Fallthrough<EnvReporting.LayerModels.DeviceInfo>();
    }
}
