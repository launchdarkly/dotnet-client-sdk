using System;
using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class MobilePollingProcessorTests : BaseTest
    {
        private const string flagsJson = "{" +
            "\"int-flag\":{\"value\":15}," +
            "\"float-flag\":{\"value\":13.5}," +
            "\"string-flag\":{\"value\":\"markw@magenic.com\"}" +
            "}";

        IFlagCacheManager mockFlagCacheManager;
        User user;

        IMobileUpdateProcessor Processor()
        {
            var mockFeatureFlagRequestor = new MockFeatureFlagRequestor(flagsJson);
            var stubbedFlagCache = new UserFlagInMemoryCache();
            mockFlagCacheManager = new MockFlagCacheManager(stubbedFlagCache);
            user = User.WithKey("user1Key");
            return new MobilePollingProcessor(mockFeatureFlagRequestor, mockFlagCacheManager, user, TimeSpan.FromSeconds(30), TimeSpan.Zero);
        }

        [Fact]
        public void CanCreateMobilePollingProcessor()
        {
            Assert.NotNull(Processor());
        }

        [Fact]
        public void StartWaitsUntilFlagCacheFilled()
        {
            var processor = Processor();
            var initTask = processor.Start();
            var unused = initTask.Wait(TimeSpan.FromSeconds(1));
            var flags = mockFlagCacheManager.FlagsForUser(user);
            Assert.Equal(3, flags.Count);
        }
    }
}
