/*
Xamarin.Essentials

The MIT License (MIT)

Copyright (c) Microsoft Corporation

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using Android.OS;
using LaunchDarkly.Sdk.EnvReporting;
using LaunchDarkly.Sdk.EnvReporting.LayerModels;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class DeviceInfo
    {
        private static IOptionalProp<OsInfo> PlatformGetOsInfo() =>
            new Props.Some<OsInfo>(new OsInfo(GetPlatform().ToString(), GetPlatform().ToString(), GetVersionString()));

        private static IOptionalProp<LaunchDarkly.Sdk.EnvReporting.LayerModels.DeviceInfo> PlatformGetDeviceInfo() =>
            new Props.Some<EnvReporting.LayerModels.DeviceInfo>(new EnvReporting.LayerModels.DeviceInfo(
                GetManufacturer(), GetModel()));
        
        
        //const int tabletCrossover = 600;

        static string GetModel() => Build.Model;

        static string GetManufacturer() => Build.Manufacturer;

        // private static string GetDeviceName()
        // {
        //     // DEVICE_NAME added in System.Global in API level 25
        //     // https://developer.android.com/reference/android/provider/Settings.Global#DEVICE_NAME
        //     var name = GetSystemSetting("device_name", true);
        //     if (string.IsNullOrWhiteSpace(name))
        //         name = ColorSpace.Model;
        //     return name;
        // }

        private static string GetVersionString() => Build.VERSION.Release;

        private static DevicePlatform GetPlatform() => DevicePlatform.Android;

        // static DeviceIdiom GetIdiom()
        // {
        //     var currentIdiom = DeviceIdiom.Unknown;
        //
        //     // first try UIModeManager
        //     using var uiModeManager = UiModeManager.FromContext(Essentials.Platform.AppContext);
        //
        //     try
        //     {
        //         var uiMode = uiModeManager?.CurrentModeType ?? UiMode.TypeUndefined;
        //         currentIdiom = DetectIdiom(uiMode);
        //     }
        //     catch (Exception ex)
        //     {
        //         System.Diagnostics.Debug.WriteLine($"Unable to detect using UiModeManager: {ex.Message}");
        //     }
        //
        //     // then try Configuration
        //     if (currentIdiom == DeviceIdiom.Unknown)
        //     {
        //         var configuration = Essentials.Platform.AppContext.Resources?.Configuration;
        //         if (configuration != null)
        //         {
        //             var minWidth = configuration.SmallestScreenWidthDp;
        //             var isWide = minWidth >= tabletCrossover;
        //             currentIdiom = isWide ? DeviceIdiom.Tablet : DeviceIdiom.Phone;
        //         }
        //         else
        //         {
        //             // start clutching at straws
        //             using var metrics = Essentials.Platform.AppContext.Resources?.DisplayMetrics;
        //             if (metrics != null)
        //             {
        //                 var minSize = Math.Min(metrics.WidthPixels, metrics.HeightPixels);
        //                 var isWide = minSize * metrics.Density >= tabletCrossover;
        //                 currentIdiom = isWide ? DeviceIdiom.Tablet : DeviceIdiom.Phone;
        //             }
        //         }
        //     }
        //
        //     // hope we got it somewhere
        //     return currentIdiom;
        // }

        // static DeviceIdiom DetectIdiom(UiMode uiMode)
        // {
        //     if (uiMode == UiMode.TypeNormal)
        //         return DeviceIdiom.Unknown;
        //     else if (uiMode == UiMode.TypeTelevision)
        //         return DeviceIdiom.TV;
        //     else if (uiMode == UiMode.TypeDesk)
        //         return DeviceIdiom.Desktop;
        //     else if (Essentials.Platform.HasApiLevel(BuildVersionCodes.KitkatWatch) && uiMode == UiMode.TypeWatch)
        //         return DeviceIdiom.Watch;
        //
        //     return DeviceIdiom.Unknown;
        // }

        // static DeviceType GetDeviceType()
        // {
        //     var isEmulator =
        //         (Build.Brand.StartsWith("generic", StringComparison.InvariantCulture) && Build.Device.StartsWith("generic", StringComparison.InvariantCulture)) ||
        //         Build.Fingerprint.StartsWith("generic", StringComparison.InvariantCulture) ||
        //         Build.Fingerprint.StartsWith("unknown", StringComparison.InvariantCulture) ||
        //         Build.Hardware.Contains("goldfish") ||
        //         Build.Hardware.Contains("ranchu") ||
        //         Build.Model.Contains("google_sdk") ||
        //         Build.Model.Contains("Emulator") ||
        //         Build.Model.Contains("Android SDK built for x86") ||
        //         Build.Manufacturer.Contains("Genymotion") ||
        //         Build.Manufacturer.Contains("VS Emulator") ||
        //         Build.Product.Contains("emulator") ||
        //         Build.Product.Contains("google_sdk") ||
        //         Build.Product.Contains("sdk") ||
        //         Build.Product.Contains("sdk_google") ||
        //         Build.Product.Contains("sdk_x86") ||
        //         Build.Product.Contains("simulator") ||
        //         Build.Product.Contains("vbox86p");
        //
        //     if (isEmulator)
        //         return DeviceType.Virtual;
        //
        //     return DeviceType.Physical;
        // }

        // private static string GetSystemSetting(string name, bool isGlobal = false)
        // {
        //     if (isGlobal && Platform.HasApiLevel(BuildVersionCodes.NMr1))
        //         return Settings.Global.GetString(Platform.AppContext.ContentResolver, name);
        //     else
        //         return Settings.System.GetString(Platform.AppContext.ContentResolver, name);
        // }
    }
}
