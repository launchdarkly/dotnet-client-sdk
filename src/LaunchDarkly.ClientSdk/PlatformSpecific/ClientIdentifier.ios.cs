using UIKit;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class ClientIdentifier
    {
        public static string Value =>
            UIDevice.CurrentDevice.IdentifierForVendor.AsString();
    }
}
