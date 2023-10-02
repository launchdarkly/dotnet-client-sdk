using System;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.EnvReporting;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class AppInfo
    {
        internal static IOptionalProp<ApplicationInfo> GetAppInfo() => PlatformGetApplicationInfo();
    }
}
