using System;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal static class Base64
    {
        public static string UrlSafeEncode(this string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes).Replace('+', '-').Replace('/', '_');
        }
    }
}
