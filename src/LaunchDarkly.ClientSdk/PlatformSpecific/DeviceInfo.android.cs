using Android.OS;
using LaunchDarkly.Sdk.EnvReporting.LayerModels;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class DeviceInfo
    {
        private static OsInfo? PlatformGetOsInfo() =>
            new OsInfo(
                DevicePlatform.Android.ToString(),
                DevicePlatform.Android.ToString()+Build.VERSION.SdkInt,
                Build.VERSION.Release
            );

        private static LaunchDarkly.Sdk.EnvReporting.LayerModels.DeviceInfo? PlatformGetDeviceInfo() =>
            new EnvReporting.LayerModels.DeviceInfo(
                Build.Manufacturer,
                Build.Model
            );
    }
}
