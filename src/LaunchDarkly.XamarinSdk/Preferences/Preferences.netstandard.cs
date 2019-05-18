using System;

namespace LaunchDarkly.Xamarin.Preferences
{
    // This code is not from Xamarin Essentials, though it implements the same Preferences abstraction.
    // It is a stub with no underlying data store.

    internal static partial class Preferences
    {
        static bool PlatformContainsKey(string key, string sharedName) => false;

        static void PlatformRemove(string key, string sharedName) { }

        static void PlatformClear(string sharedName) { }

        static void PlatformSet<T>(string key, T value, string sharedName) { }

        static T PlatformGet<T>(string key, T defaultValue, string sharedName) => defaultValue;
    }
}
