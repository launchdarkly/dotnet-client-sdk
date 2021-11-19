using LaunchDarkly.Logging;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class ClientIdentifier
    {
        // Unlike mobile platforms, .NET standard doesn't have an OS-based notion of a device identifier.
        public static string Value => null;
    }
}
