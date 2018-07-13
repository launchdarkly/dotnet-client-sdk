using System;
using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class MobilePollingProcessorTests
    {
        IFlagCacheManager mockFlagCacheManager;
        User user;

        IMobileUpdateProcessor Processor()
        {
            var mockFeatureFlagRequestor = new MockFeatureFlagRequestor();
            var stubbedFlagCache = new UserFlagInMemoryCache();
            mockFlagCacheManager = new MockFlagCacheManager(stubbedFlagCache);
            user = User.WithKey("user1Key");
            var timeSpan = TimeSpan.FromSeconds(1);
            return new MobilePollingProcessor(mockFeatureFlagRequestor, mockFlagCacheManager, user, timeSpan);
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
            Assert.Equal(6, flags.Count);
        }
    }
}
