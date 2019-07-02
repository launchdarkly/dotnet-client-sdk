
namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class ClientIdentifier
    {
    	// Unlike mobile platforms, .NET standard doesn't have an OS-based notion of a device identifier.
    	// Instead, we'll do what we do in the non-mobile client-side SDKs: see if we've already cached a
    	// user key for this (OS) user account, and if not, generate a randomized ID and cache it.
        public static string PlatformGetOrCreateClientId()
        {
            return GetOrCreateRandomizedClientId();
        }
    }
}
