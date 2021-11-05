using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal
{
    // This delegates to the conditionally-compiled code in LaunchDarkly.Sdk.Client.PlatformSpecific
    // to get the device identifier. The only reason it is a pluggable component is for unit tests;
    // we don't currently expose IDeviceInfo.

    internal sealed class DefaultDeviceInfo : IDeviceInfo
    {
        public string UniqueDeviceId() => PlatformSpecific.ClientIdentifier.Value;
    }
}
