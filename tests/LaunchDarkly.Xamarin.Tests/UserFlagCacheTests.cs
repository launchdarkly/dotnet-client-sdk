using System;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class UserFlagCacheTests
    {
        IUserFlagCache inMemoryCache = new UserFlagInMemoryCache();
        User user1 = User.WithKey("user1Key");
        User user2 = User.WithKey("user2Key");

        [Fact]
        public void CanCacheFlagsInMemory()
        {
            var text = JSONReader.FeatureFlagJSON();
            var flags = JsonConvert.DeserializeObject<IDictionary<string, FeatureFlag>>(text);
            inMemoryCache.CacheFlagsForUser(flags, user1);
            var flagsRetrieved = inMemoryCache.RetrieveFlags(user1);
            Assert.Equal(flags.Count, flagsRetrieved.Count);
            var secondFlag = flags.Values.ToList()[1];
            var secondFlagRetrieved = flagsRetrieved.Values.ToList()[1];
            Assert.Equal(secondFlag, secondFlagRetrieved);
        }
    }
}
