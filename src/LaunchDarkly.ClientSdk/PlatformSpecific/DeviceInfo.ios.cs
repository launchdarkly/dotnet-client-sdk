using LaunchDarkly.Sdk.EnvReporting.LayerModels;
#if __WATCHOS__
using WatchKit;
using UIDevice = WatchKit.WKInterfaceDevice;
#else
using UIKit;
#endif

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class DeviceInfo
    {

        private static OsInfo? PlatformGetOsInfo() =>
            new OsInfo("Apple", GetPlatform().ToString(), UIDevice.CurrentDevice.SystemVersion);

        private static LaunchDarkly.Sdk.EnvReporting.LayerModels.DeviceInfo? PlatformGetDeviceInfo() =>
            new EnvReporting.LayerModels.DeviceInfo(
                "Apple", UIDevice.CurrentDevice.Model);
        static DevicePlatform GetPlatform() =>
#if __IOS__
            DevicePlatform.iOS;
#elif __TVOS__
            DevicePlatform.tvOS;
#elif __WATCHOS__
            DevicePlatform.watchOS;
#endif
    }
}
