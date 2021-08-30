using System;
using System.Net.Http;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class Http
    {
        /// <summary>
        /// If our default configuration should use a specific <see cref="HttpMessageHandler"/>
        /// implementation, returns that implementation.
        /// </summary>
        /// <remarks>
        /// The timeouts are passed in because the Xamarin Android implementation does not actually
        /// look at the configured timeouts from HttpClient.
        /// </remarks>
        /// <returns>an HTTP handler implementation or null</returns>
        public static HttpMessageHandler CreateHttpMessageHandler(TimeSpan connectTimeout, TimeSpan readTimeout) =>
            PlatformCreateHttpMessageHandler(connectTimeout, readTimeout);

        /// <summary>
        /// Converts any platform-specific exceptions that might be thrown by the platform-specific
        /// HTTP handler to their .NET equivalents.
        /// </summary>
        /// <remarks>
        /// We don't really care about specific network exception classes in our code, but in any case
        /// where we might expose the exception to application code, we want to normalize it to use only
        /// .NET classes.
        /// </remarks>
        /// <param name="e">an exception</param>
        /// <returns>the same exception or a more .NET-appropriate one</returns>
        public static Exception TranslateHttpException(Exception e) =>
            e is HttpRequestException ? e : PlatformTranslateHttpException(e);
    }
}
