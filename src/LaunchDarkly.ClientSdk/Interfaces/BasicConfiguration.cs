
namespace LaunchDarkly.Sdk.Client.Interfaces
{
    /// <summary>
    /// The most basic properties of the SDK client that are available to all SDK components.
    /// </summary>
    public sealed class BasicConfiguration
    {
        /// <summary>
        /// The configured mobile key.
        /// </summary>
        public string MobileKey { get; }

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="mobileKey">the configured mobile key</param>
        public BasicConfiguration(string mobileKey)
        {
            MobileKey = mobileKey;
        }
    }
}
