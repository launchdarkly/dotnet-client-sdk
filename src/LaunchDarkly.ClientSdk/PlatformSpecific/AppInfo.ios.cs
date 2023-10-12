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
        static ApplicationInfo? PlatformGetApplicationInfo() => new ApplicationInfo(
            GetBundleValue("CFBundleIdentifier"),
            GetBundleValue("CFBundleName"),
            GetBundleValue("CFBundleVersion"),
            GetBundleValue("CFBundleShortString"));

        static string GetBundleValue(string key)
            => NSBundle.MainBundle.ObjectForInfoDictionary(key)?.ToString();
    }
}
