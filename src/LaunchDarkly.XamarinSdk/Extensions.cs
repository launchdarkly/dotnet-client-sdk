using System;
using LaunchDarkly.Client;

namespace LaunchDarkly.Xamarin
{
    internal static class Extensions
    {
        public static string UrlSafeBase64Encode(this string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes).Replace('+', '-').Replace('/', '_');
        }

        public static string AsJson(this User user)
        {
            return JsonUtil.EncodeJson(user);
        }
    }
}
