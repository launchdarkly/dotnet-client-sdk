using System;
using System.Net.Http;

namespace LaunchDarkly.Sdk.Xamarin.PlatformSpecific
{
    internal static partial class Http
    {
        private static HttpMessageHandler PlatformCreateHttpMessageHandler(TimeSpan connectTimeout, TimeSpan readTimeout) =>
            null;
            // Setting the HttpClient's message handler to null means it will use the default .NET implementation,
            // which already correctly implements timeouts based on the client configuration.

        private static Exception PlatformTranslateHttpException(Exception e) => e;
    }
}
