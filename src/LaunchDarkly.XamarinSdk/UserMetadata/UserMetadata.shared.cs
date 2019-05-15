
namespace LaunchDarkly.Xamarin
{
    internal static partial class UserMetadata
    {
        private static readonly string _os = GetOS();
        private static readonly string _device = GetDevice();

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
