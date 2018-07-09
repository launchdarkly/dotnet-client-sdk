using System;
using System.Collections.Generic;
using LaunchDarkly.Client;
using Newtonsoft.Json;

namespace LaunchDarkly.Xamarin.Tests
{
    public static class JSONReader
    {
        public static string FeatureFlagJSON()
        {
            return JSONTextFromFile("FeatureFlag.json");
        }

        public static string FeatureFlagJSONFromService()
        {
            return JSONTextFromFile("FeatureFlagsFromService.json");
        }

        public static string JSONTextFromFile(string filename)
        {
            return System.IO.File.ReadAllText(filename);
        }

        internal static IUserFlagCache StubbedFlagCache(User user)
        {
            // stub json into the FlagCache
            IUserFlagCache stubbedFlagCache = new UserFlagInMemoryCache();
            if (String.IsNullOrEmpty(user.Key))
                return stubbedFlagCache;
            
            stubbedFlagCache.CacheFlagsForUser(StubbedFlagsDictionary(), user);
            return stubbedFlagCache;
        }

        internal static IDictionary<string, FeatureFlag> StubbedFlagsDictionary()
        {
            var text = FeatureFlagJSONFromService();
            var flags = JsonConvert.DeserializeObject<IDictionary<string, FeatureFlag>>(text);
            return flags;
        }

        internal static IDictionary<string, FeatureFlag> UpdatedStubbedFlagsDictionary()
        {
            var text = FeatureFlagJSON();
            var flags = JsonConvert.DeserializeObject<IDictionary<string, FeatureFlag>>(text);
            return flags;
        }
    }
}
