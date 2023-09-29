using LaunchDarkly.Sdk.EnvReporting;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class AppInfo
    {
        private static IProp<ApplicationInfo> PlatformGetApplicationInfo() => new Props.Fallthrough<ApplicationInfo>();
    }
}
