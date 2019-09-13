using System;
using System.Net.Http;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class Http
    {
        private static HttpMessageHandler PlatformCreateHttpMessageHandler(TimeSpan connectTimeout, TimeSpan readTimeout) =>
            new NSUrlSessionHandler();

        // NSUrlSessionHandler doesn't appear to throw platform-specific exceptions that we care about
        private static Exception PlatformTranslateHttpException(Exception e) => e;
    }
}
