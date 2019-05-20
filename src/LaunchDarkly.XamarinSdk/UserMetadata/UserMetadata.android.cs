using Android.OS;

namespace LaunchDarkly.Xamarin
{
    internal static partial class UserMetadata
    {
        private static string PlatformDevice =>
            Build.Model + " " + Build.Product;

        private static string PlatformOS =>
            "Android " + Build.VERSION.SdkInt;
    }
}
