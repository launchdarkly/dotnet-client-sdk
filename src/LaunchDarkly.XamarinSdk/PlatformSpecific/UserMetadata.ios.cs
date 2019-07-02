using UIKit;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class UserMetadata
    {
        private static string PlatformDevice
        {
            get
            {
                switch (UIDevice.CurrentDevice.UserInterfaceIdiom)
                {
                    case UIUserInterfaceIdiom.CarPlay:
                        return "CarPlay";
                    case UIUserInterfaceIdiom.Pad:
                        return "iPad";
                    case UIUserInterfaceIdiom.Phone:
                        return "iPhone";
                    case UIUserInterfaceIdiom.TV:
                        return "Apple TV";
                    default:
                        return "unknown";
                }
            }
        }

        private static string PlatformOS =>
            "iOS " + UIDevice.CurrentDevice.SystemVersion;
    }
}
