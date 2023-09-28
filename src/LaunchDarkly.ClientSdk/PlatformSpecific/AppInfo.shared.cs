using System;
using LaunchDarkly.Sdk.Client.Internal;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class AppInfo
    {
        internal static ApplicationInfo? Get() => PlatformGet();
    }
}
