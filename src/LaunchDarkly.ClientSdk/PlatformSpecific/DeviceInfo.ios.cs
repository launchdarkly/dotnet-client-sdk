using System;
using System.Diagnostics;
using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.EnvReporting.LayerModels;
#if __WATCHOS__
using WatchKit;
using UIDevice = WatchKit.WKInterfaceDevice;
#else
using UIKit;
#endif

using ObjCRuntime;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class DeviceInfo
    {

        private static OsInfo? PlatformGetOsInfo() =>
            new OsInfo(GetManufacturer(), GetPlatform().ToString(), GetVersionString());

        private static LaunchDarkly.Sdk.EnvReporting.LayerModels.DeviceInfo? PlatformGetDeviceInfo() =>
            new EnvReporting.LayerModels.DeviceInfo(
                GetManufacturer(), GetModel());

        static string GetModel() => UIDevice.CurrentDevice.Model;

        static string GetManufacturer() => "Apple";
        
        static string GetVersionString() => UIDevice.CurrentDevice.SystemVersion;

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
