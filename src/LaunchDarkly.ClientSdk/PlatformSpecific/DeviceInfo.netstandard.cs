using System.Runtime.InteropServices;
using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.EnvReporting.LayerModels;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class DeviceInfo
    {
        private static OsInfo? PlatformGetOsInfo()
        {
            var osName = "unknown";
            var osFamily = "unknown";
            var osVersion = "unknown";
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                osName = OSPlatform.Linux.ToString();
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                osName = OSPlatform.Windows.ToString();
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                osName = OSPlatform.OSX.ToString();
            }

            return new OsInfo(osFamily, osName, osVersion);
        }

        private static LaunchDarkly.Sdk.EnvReporting.LayerModels.DeviceInfo? PlatformGetDeviceInfo() => null;
    }
}
