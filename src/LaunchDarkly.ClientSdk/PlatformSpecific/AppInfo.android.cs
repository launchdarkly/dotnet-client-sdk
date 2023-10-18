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

using System.Globalization;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Provider;
using LaunchDarkly.Sdk.EnvReporting;
#if __ANDROID_29__
using AndroidX.Core.Content.PM;
#else
using Android.Support.V4.Content.PM;
#endif

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class AppInfo
    {
        static ApplicationInfo? PlatformGetApplicationInfo() => new ApplicationInfo(
            PlatformGetAppId(),
            PlatformGetAppName(),
            PlatformGetAppVersion(),
            PlatformGetAppVersionName());

        // The following methods are added by LaunchDarkly to align with the Application Info
        // required by the SDK.
        static string PlatformGetAppId() => Platform.AppContext.PackageName;
        static string PlatformGetAppName() => PlatformGetName();
        static string PlatformGetAppVersion() => PlatformGetBuild();
        static string PlatformGetAppVersionName() => PlatformGetVersionString();

        // End LaunchDarkly additions.

        static string PlatformGetName()
        {
            var applicationInfo = Platform.AppContext.ApplicationInfo;
            var packageManager = Platform.AppContext.PackageManager;
            return applicationInfo.LoadLabel(packageManager);
        }

        static string PlatformGetVersionString()
        {
            var pm = Platform.AppContext.PackageManager;
            var packageName = Platform.AppContext.PackageName;
#pragma warning disable CS0618 // Type or member is obsolete
            using (var info = pm.GetPackageInfo(packageName, PackageInfoFlags.MetaData))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                return info.VersionName;
            }
        }

        static string PlatformGetBuild()
        {
            var pm = Platform.AppContext.PackageManager;
            var packageName = Platform.AppContext.PackageName;
#pragma warning disable CS0618 // Type or member is obsolete
            using (var info = pm.GetPackageInfo(packageName, PackageInfoFlags.MetaData))
#pragma warning restore CS0618 // Type or member is obsolete
            {
#if __ANDROID_28__
                return PackageInfoCompat.GetLongVersionCode(info).ToString(CultureInfo.InvariantCulture);
#else
#pragma warning disable CS0618 // Type or member is obsolete
                return info.VersionCode.ToString(CultureInfo.InvariantCulture);
#pragma warning restore CS0618 // Type or member is obsolete
#endif
            }
        }
    }
}
