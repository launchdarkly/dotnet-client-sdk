using Android.OS;

namespace LaunchDarkly.Xamarin.PlatformSpecific
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
