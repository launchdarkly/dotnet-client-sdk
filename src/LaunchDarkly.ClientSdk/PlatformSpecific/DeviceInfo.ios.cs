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
        static string GetModel()
        {
            try
            {
                return Platform.GetSystemLibraryProperty("hw.machine");
            }
            catch (Exception)
            {
                Debug.WriteLine("Unable to query hardware model, returning current device model.");
            }
            return UIDevice.CurrentDevice.Model;
        }

        static string GetManufacturer() => "Apple";

        static string GetDeviceName() => UIDevice.CurrentDevice.Name;

        static string GetVersionString() => UIDevice.CurrentDevice.SystemVersion;

        static DevicePlatform GetPlatform() =>
#if __IOS__
            DevicePlatform.iOS;
#elif __TVOS__
            DevicePlatform.tvOS;
#elif __WATCHOS__
            DevicePlatform.watchOS;
#endif

//         static DeviceIdiom GetIdiom()
//         {
// #if __WATCHOS__
//             return DeviceIdiom.Watch;
// #else
//             switch (UIDevice.CurrentDevice.UserInterfaceIdiom)
//             {
//                 case UIUserInterfaceIdiom.Pad:
//                     return DeviceIdiom.Tablet;
//                 case UIUserInterfaceIdiom.Phone:
//                     return DeviceIdiom.Phone;
//                 case UIUserInterfaceIdiom.TV:
//                     return DeviceIdiom.TV;
//                 case UIUserInterfaceIdiom.CarPlay:
//                 case UIUserInterfaceIdiom.Unspecified:
//                 default:
//                     return DeviceIdiom.Unknown;
//             }
// #endif
//         }

        // static DeviceType GetDeviceType()
        //     => Runtime.Arch == Arch.DEVICE ? DeviceType.Physical : DeviceType.Virtual;
    }
}
