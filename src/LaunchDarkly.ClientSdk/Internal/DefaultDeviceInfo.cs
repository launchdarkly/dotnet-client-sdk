using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.PlatformSpecific;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;

namespace LaunchDarkly.Sdk.Client.Internal
{
    // This just delegates to the conditionally-compiled code in LaunchDarkly.Sdk.Client.PlatformSpecific.
    // The only reason it is a pluggable component is for unit tests; we don't currently expose IDeviceInfo.
    internal sealed class DefaultDeviceInfo : IDeviceInfo
    {
        private readonly Logger _log;

        internal DefaultDeviceInfo(Logger log)
        {
            _log = log;
        }

        public string UniqueDeviceId() =>
            ClientIdentifier.GetOrCreateClientId(_log);
    }
}
