using Android.OS;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class UserMetadata
    {
        private static string PlatformDevice =>
            Build.Model + " " + Build.Product;

        private static string PlatformOS =>
            "Android " + Build.VERSION.SdkInt;

        private static PlatformType PlatformPlatformType => PlatformType.Android;
    }
}
