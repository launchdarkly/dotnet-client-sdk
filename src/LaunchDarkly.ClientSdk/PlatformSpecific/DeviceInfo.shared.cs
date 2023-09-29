using System;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.EnvReporting.LayerModels;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class DeviceInfo
    {
        internal static IProp<OsInfo> GetOsInfo() => PlatformGetOsInfo();

        internal static IProp<LaunchDarkly.Sdk.EnvReporting.LayerModels.DeviceInfo> GetDeviceInfo() => PlatformGetDeviceInfo();
    }
}
