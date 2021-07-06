
namespace LaunchDarkly.Sdk.Xamarin
{
    /// <summary>
    /// Values returned by <see cref="LdClient.PlatformType"/>.
    /// </summary>
    public enum PlatformType
    {
        /// <summary>
        /// You are using the .NET Standard version of the SDK.
        /// </summary>
        Standard,

        /// <summary>
        /// You are using the Android version of the SDK.
        /// </summary>
        Android,

        /// <summary>
        /// You are using the iOS version of the SDK.
        /// </summary>
        IOs
    }
}
