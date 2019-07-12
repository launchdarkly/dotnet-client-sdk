using System.Collections.Generic;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    // This code is not from Xamarin Essentials, though it implements the same Connectivity abstraction.
    // It is a stub that always reports that we do have network connectivity.
    //
    // Unfortunately, in .NET Standard that is the best we can do. There is (at least in 2.0) a
    // NetworkInterface.GetIsNetworkAvailable() method, but that doesn't test whether we actuually have
    // Internet access, just whether we have a network interface (i.e. if we're running a desktop app
    // on a laptop, and the wi-fi is turned off, it will still return true as long as the laptop has an
    // Ethernet card-- even if it's not plugged in).
    //
    // So, in order to support connectivity detection on non-mobile platforms, we would need to add more
    // platform-specific variants. For instance, here's how Xamarin Essentials does it for UWP:
    //   https://github.com/xamarin/Essentials/blob/master/Xamarin.Essentials/Connectivity/Connectivity.uwp.cs

    internal static partial class Connectivity
    {
        static NetworkAccess PlatformNetworkAccess => NetworkAccess.Internet;

        static IEnumerable<ConnectionProfile> PlatformConnectionProfiles
        {
            get
            {
                yield return ConnectionProfile.Unknown;
            }
        }

        static void StartListeners() { }

        static void StopListeners() { }
    }
}
