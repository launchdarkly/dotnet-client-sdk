using System;
using System.Security.Cryptography;
using System.Text;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal static class Base64
    {
        private static readonly SHA256 _hasher = SHA256.Create();

        public static string UrlSafeEncode(this string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes).Replace('+', '-').Replace('/', '_');
        }

        public static string Sha256Hash(string input) =>
            Convert.ToBase64String(
                _hasher.ComputeHash(Encoding.UTF8.GetBytes(input))
                );
    }
}
