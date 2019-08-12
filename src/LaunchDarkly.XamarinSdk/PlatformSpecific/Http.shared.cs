using System.Net.Http;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class Http
    {
        private static HttpMessageHandler _httpMessageHandler = PlatformCreateHttpMessageHandler();

        /// <summary>
        /// If our default configuration should use a specific <see cref="HttpMessageHandler"/>
        /// implementation, returns that implementation.
        /// </summary>
        /// <remarks>
        /// The handler is not stateful, so it can be a shared instance. If we don't need to use a
        /// specific implementation, this returns <c>null</c>. This is just the default for
        /// <see cref="ConfigurationBuilder"/>, so the application can still override it. If it is
        /// <c>null</c> and the application lets it remain <c>null</c>, then Xamarin will make its
        /// own decision based on logic we don't have access to; in practice that seems to result
        /// in picking the same handler that our platform-specific logic would specify, but that
        /// may be dependent on project configuration, so we decided to explicitly set a default.
        /// </remarks>
        /// <returns>an HTTP handler implementation or null</returns>
        public static HttpMessageHandler GetHttpMessageHandler() => _httpMessageHandler;
    }
}
