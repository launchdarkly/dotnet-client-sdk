using System;
using LaunchDarkly.Logging;

namespace LaunchDarkly.Sdk.Xamarin.PlatformSpecific
{
    internal static partial class ClientIdentifier
    {
        private static volatile string _id;

        private const string PreferencesAnonUserIdKey = "anonUserId";

        public static string GetOrCreateClientId(Logger log)
        {
            var id = _id;
            if (id is null)
            {
                id = PlatformGetOrCreateClientId(log);
                _id = id;
            }
            return id;
        }

        // Used only for testing, to keep previous calls to GetOrCreateRandomizedClientId from affecting test state.
        // On mobile platforms this has no effect.
        internal static void ClearCachedClientId(Logger log)
        {
            Preferences.Remove(PreferencesAnonUserIdKey, log);
        }

        private static string GetOrCreateRandomizedClientId(Logger log)
        {
            // On non-mobile platforms, there may not be an OS-based notion of a device identifier. Instead,
            // we'll do what we do in the non-mobile client-side SDKs: see if we've already cached a user key
            // for this user account (OS user, that is), and if not, generate a randomized ID and cache it.
            string cachedKey = Preferences.Get(PreferencesAnonUserIdKey, null, log);
            if (cachedKey != null)
            {
                return cachedKey;
            }
            string guid = Guid.NewGuid().ToString();
            Preferences.Set(PreferencesAnonUserIdKey, guid, log);
            return guid;
        }
    }
}
