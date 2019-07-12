using System;
using System.Collections.Generic;
using LaunchDarkly.Client;
using Newtonsoft.Json;

namespace LaunchDarkly.Xamarin
{
    public static class Extensions
    {
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string AsJson(this User user)
        {
            var userAsString = JsonConvert.SerializeObject(user);
            return userAsString;
        }
    }
}
