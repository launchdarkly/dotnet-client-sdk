using LaunchDarkly.Sdk.Internal;

namespace LaunchDarkly.Sdk.Client.Internal
{
    
    /// <summary>
    /// Defines common information about the SDK itself for usage
    /// in various components.
    /// </summary>
    internal static class SdkPackage
    {
        /// <summary>
        /// The canonical name of this SDK, following the convention of (technology)-(server|client)-sdk.
        /// </summary>
        internal const string Name = "dotnet-client-sdk";

        /// <summary>
        /// The prefix for the User-Agent header, omitting the version string. This may be different than the Name
        /// due to historical reasons. 
        /// </summary>
        private const string UserAgentPrefix = "XamarinClient";

        /// <summary>
        /// Version of the SDK.
        /// </summary>
        internal static string Version => AssemblyVersions.GetAssemblyVersionStringForType(typeof(LdClient));
        
        /// <summary>
        /// User-Agent suitable for usage in HTTP requests. 
        /// </summary>
        internal static string UserAgent => $"{UserAgentPrefix}/{Version}";
        
    }
}
