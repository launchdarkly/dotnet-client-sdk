using LaunchDarkly.Sdk.EnvReporting;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class AppInfo
    {
        private static IOptionalProp<ApplicationInfo> PlatformGetApplicationInfo() => new Props.None<ApplicationInfo>();
    }
}
