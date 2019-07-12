
namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    // This class and the rest of its partial class implementations are not derived from Xamarin Essentials.

    internal static partial class UserMetadata
    {
        // These values are obtained from the platform-specific code once and then stored in static fields,
        // to avoid having to recompute them many times.
        private static readonly string _os = PlatformOS;
        private static readonly string _device = PlatformDevice;

        /// <summary>
        /// Returns the string that should be passed in the "device" property for all users.
        /// </summary>
        /// <returns>The value for "device", or null if none.</returns>
        internal static string DeviceName => _device;

        /// <summary>
        /// Returns the string that should be passed in the "os" property for all users.
        /// </summary>
        /// <returns>The value for "os", or null if none.</returns>
        internal static string OSName => _os;
    }
}
