using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.EnvReporting.LayerModels;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class DeviceInfo
    {
        private static IOptionalProp<OsInfo> PlatformGetOsInfo() => new Props.None<OsInfo>();

        private static IOptionalProp<LaunchDarkly.Sdk.EnvReporting.LayerModels.DeviceInfo> PlatformGetDeviceInfo() =>
            new Props.None<EnvReporting.LayerModels.DeviceInfo>();
    }
}
