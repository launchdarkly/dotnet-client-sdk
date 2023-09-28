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
using System.Collections.Generic;
using System.Linq;
using Foundation;
#if __IOS__ || __TVOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class AppInfo
    {
        
        static string PlatformGetPackageName() => GetBundleValue("CFBundleIdentifier");
        
        static string PlatformGetName() => GetBundleValue("CFBundleDisplayName") ?? GetBundleValue("CFBundleName");
        
        static string PlatformGetVersionString() => GetBundleValue("CFBundleShortVersionString");
        
        static string PlatformGetBuild() => GetBundleValue("CFBundleVersion");

        static string GetBundleValue(string key)
           => NSBundle.MainBundle.ObjectForInfoDictionary(key)?.ToString();

// #if __IOS__ || __TVOS__
//         static async void PlatformShowSettingsUI()
//             => await Launcher.OpenAsync(UIApplication.OpenSettingsUrlString);
// #elif __MACOS__
//         static void PlatformShowSettingsUI()
//         {
//             MainThread.BeginInvokeOnMainThread(() =>
//             {
// // Ignore obsolete for the time being. Will be updated in a future release.
// #pragma warning disable CS0618 // Type or member is obsolete
//                 var prefsApp = ScriptingBridge.SBApplication.FromBundleIdentifier("com.apple.systempreferences");
// #pragma warning restore CS0618 // Type or member is obsolete
//                 prefsApp.SendMode = ScriptingBridge.AESendMode.NoReply;
//                 prefsApp.Activate();
//             });
//         }
// #else
//         static void PlatformShowSettingsUI() =>
//             throw new FeatureNotSupportedException();
// #endif

// #if __IOS__ || __TVOS__
//         static AppTheme PlatformRequestedTheme()
//         {
//             if (!Platform.HasOSVersion(13, 0))
//                 return AppTheme.Unspecified;
//
//             var uiStyle = Platform.GetCurrentUIViewController()?.TraitCollection?.UserInterfaceStyle ??
//                 UITraitCollection.CurrentTraitCollection.UserInterfaceStyle;
//
//             return uiStyle switch
//             {
//                 UIUserInterfaceStyle.Light => AppTheme.Light,
//                 UIUserInterfaceStyle.Dark => AppTheme.Dark,
//                 _ => AppTheme.Unspecified
//             };
//         }
// #elif __MACOS__
//         static AppTheme PlatformRequestedTheme()
//         {
//             if (DeviceInfo.Version >= new Version(10, 14))
//             {
//                 var app = NSAppearance.CurrentAppearance?.FindBestMatch(new string[]
//                 {
//                     NSAppearance.NameAqua,
//                     NSAppearance.NameDarkAqua
//                 });
//
//                 if (string.IsNullOrEmpty(app))
//                     return AppTheme.Unspecified;
//
//                 if (app == NSAppearance.NameDarkAqua)
//                     return AppTheme.Dark;
//             }
//             return AppTheme.Light;
//         }
// #else
//         static AppTheme PlatformRequestedTheme() =>
//             AppTheme.Unspecified;
// #endif

        internal static bool VerifyHasUrlScheme(string scheme)
        {
            var cleansed = scheme.Replace("://", string.Empty);
            var schemes = GetCFBundleURLSchemes().ToList();
            return schemes.Any(x => x != null && x.Equals(cleansed, StringComparison.InvariantCultureIgnoreCase));
        }

        internal static IEnumerable<string> GetCFBundleURLSchemes()
        {
            var schemes = new List<string>();

            NSObject nsobj = null;
            if (!NSBundle.MainBundle.InfoDictionary.TryGetValue((NSString)"CFBundleURLTypes", out nsobj))
                return schemes;

            var array = nsobj as NSArray;

            if (array == null)
                return schemes;

            for (nuint i = 0; i < array.Count; i++)
            {
                var d = array.GetItem<NSDictionary>(i);
                if (d == null || !d.Any())
                    continue;

                if (!d.TryGetValue((NSString)"CFBundleURLSchemes", out nsobj))
                    continue;

                var a = nsobj as NSArray;
                var urls = ConvertToIEnumerable<NSString>(a).Select(x => x.ToString()).ToArray();
                foreach (var url in urls)
                    schemes.Add(url);
            }

            return schemes;
        }

        static IEnumerable<T> ConvertToIEnumerable<T>(NSArray array)
            where T : class, ObjCRuntime.INativeObject
        {
            for (nuint i = 0; i < array.Count; i++)
                yield return array.GetItem<T>(i);
        }
    }
}