using LaunchDarkly.Logging;
using UIKit;

namespace LaunchDarkly.Sdk.Xamarin.PlatformSpecific
{
    internal static partial class ClientIdentifier
    {
        // For mobile platforms that really have a device ID, we delegate to Plugin.DeviceInfo to get the ID.
        private static string PlatformGetOrCreateClientId(Logger log)
        {
            return UIDevice.CurrentDevice.IdentifierForVendor.AsString();
        }
    }
}
