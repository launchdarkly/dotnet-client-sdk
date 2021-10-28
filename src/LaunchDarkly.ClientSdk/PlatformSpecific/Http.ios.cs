using System;
using System.Net;
using System.Net.Http;
using LaunchDarkly.Sdk.Internal.Http;

namespace LaunchDarkly.Sdk.Client.PlatformSpecific
{
    internal static partial class Http
    {
        // NSUrlSessionHandler is the preferred native implementation of HttpMessageHandler on iOS.
        // However, it does not support programmatically setting a proxy, so if a proxy was specified
        // we must fall back to the non-native .NET implementation.
        private static Func<HttpProperties, HttpMessageHandler> PlatformGetHttpMessageHandlerFactory(
            TimeSpan connectTimeout,
            TimeSpan readTimeout,
            IWebProxy proxy
            ) =>
            (proxy is null)
            ? (p => (HttpMessageHandler)new NSUrlSessionHandler())
            : (Func<HttpProperties, HttpMessageHandler>)null;

        // NSUrlSessionHandler doesn't appear to throw platform-specific exceptions that we care about
        private static Exception PlatformTranslateHttpException(Exception e) => e;
    }
}
