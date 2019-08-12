using System.Net.Http;
using Xamarin.Android.Net;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class Http
    {
        private static HttpMessageHandler PlatformCreateHttpMessageHandler() =>
            new AndroidClientHandler();
    }
}
