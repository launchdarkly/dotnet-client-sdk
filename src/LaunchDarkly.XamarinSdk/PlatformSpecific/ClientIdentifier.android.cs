using Plugin.DeviceInfo;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class ClientIdentifier
    {
    	// For mobile platforms that really have a device ID, we delegate to Plugin.DeviceInfo to get the ID.
        public static string PlatformGetOrCreateClientId()
        {
            return CrossDeviceInfo.Current.Id();
        }
    }
}
