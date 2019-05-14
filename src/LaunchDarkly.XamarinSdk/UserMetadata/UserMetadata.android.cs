using Android.OS;

namespace LaunchDarkly.Xamarin
{
    internal static partial class UserMetadata
    {
        private static string GetDevice()
        {
            return Build.Model + " " + Build.Product;
        }

        private static string GetOS()
        {
            return "Android " + Build.VERSION.SdkInt;
        }
    }
}
