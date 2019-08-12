using System.Net.Http;

namespace LaunchDarkly.Xamarin.PlatformSpecific
{
    internal static partial class Http
    {
        private static HttpMessageHandler PlatformCreateHttpMessageHandler() => null;
    }
}
