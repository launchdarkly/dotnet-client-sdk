using UIKit;

namespace LaunchDarkly.Xamarin
{
    internal static partial class UserMetadata
    {
        private static string GetDevice()
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

        private static string GetOS()
        {
            return "iOS " + UIDevice.CurrentDevice.SystemVersion;
        }
    }
}
