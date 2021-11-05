
namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    // This class and the rest of its partial class implementations are not derived from Xamarin Essentials.

    internal static partial class UserMetadata
    {
        /// <summary>
        /// Returns the string that should be passed in the "device" property for all users.
        /// </summary>
        /// <returns>The value for "device", or null if none.</returns>
        internal static string DeviceName => PlatformDevice;

        /// <summary>
        /// Returns the string that should be passed in the "os" property for all users.
        /// </summary>
        /// <returns>The value for "os", or null if none.</returns>
        internal static string OSName => PlatformOS;

        internal static PlatformType PlatformType => PlatformPlatformType;
    }
}
