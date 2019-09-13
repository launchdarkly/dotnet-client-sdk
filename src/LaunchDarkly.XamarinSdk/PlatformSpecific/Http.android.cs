using System;
using System.Net.Http;
using Xamarin.Android.Net;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class Http
    {
        private static HttpMessageHandler PlatformCreateHttpMessageHandler(TimeSpan connectTimeout, TimeSpan readTimeout) =>
            new AndroidClientHandler()
            {
                ConnectTimeout = connectTimeout,
                ReadTimeout = readTimeout
            };

        private static Exception PlatformTranslateHttpException(Exception e)
        {
            if (e is Java.Net.SocketTimeoutException)
            {
                return new TimeoutException();
            }
            return e;
        }
    }
}
