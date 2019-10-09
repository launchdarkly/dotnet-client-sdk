using LaunchDarkly.Xamarin.PlatformSpecific;

namespace LaunchDarkly.Xamarin
{
    // This just delegates to the conditionally-compiled code in LaunchDarkly.Xamarin.PlatformSpecific.
    // The only reason it is a pluggable component is for unit tests; we don't currently expose IDeviceInfo.
    internal sealed class DefaultDeviceInfo : IDeviceInfo
    {
        public string UniqueDeviceId()
        {
            return ClientIdentifier.GetOrCreateClientId();
        }
    }
}
