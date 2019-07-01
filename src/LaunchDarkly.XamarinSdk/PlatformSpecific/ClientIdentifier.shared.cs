using System;
using LaunchDarkly.Xamarin.Preferences;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class ClientIdentifier
    {
        private const string PreferencesAnonUserIdKey = "anonUserId";

        public static string GetOrCreateClientId()
        {
            return PlatformGetOrCreateClientId();
        }

        // Used only for testing, to keep previous calls to GetOrCreateRandomizedClientId from affecting test state.
        // On mobile platforms this has no effect.
        internal static void ClearCachedClientId()
        {
            Preferences.Preferences.Clear(PreferencesAnonUserIdKey);
        }

        private static string GetOrCreateRandomizedClientId()
        {
            // On non-mobile platforms, there may not be an OS-based notion of a device identifier. Instead,
            // we'll do what we do in the non-mobile client-side SDKs: see if we've already cached a user key
            // for this user account (OS user, that is), and if not, generate a randomized ID and cache it.
            string cachedKey = Preferences.Preferences.Get(PreferencesAnonUserIdKey, null);
            if (cachedKey != null)
            {
                return cachedKey;
            }
            string guid = Guid.NewGuid().ToString();
            Preferences.Preferences.Set(PreferencesAnonUserIdKey, guid);
            return guid;
        }
    }
}
