using System;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    public class PollingDataSourceTest : BaseTest
    {
        private const string flagsJson = "{" +
            "\"int-flag\":{\"value\":15}," +
            "\"float-flag\":{\"value\":13.5}," +
            "\"string-flag\":{\"value\":\"markw@magenic.com\"}" +
            "}";

        IFlagCacheManager mockFlagCacheManager;
        User user;

        public PollingDataSourceTest(ITestOutputHelper testOutput) : base(testOutput) { }

        IDataSource MakeDataSource()
        {
            var mockFeatureFlagRequestor = new MockFeatureFlagRequestor(flagsJson);
            var stubbedFlagCache = new UserFlagInMemoryCache();
            mockFlagCacheManager = new MockFlagCacheManager(stubbedFlagCache);
            user = User.WithKey("user1Key");
            return new PollingDataSource(
                new DataSourceUpdateSinkImpl(mockFlagCacheManager),
                user,
                mockFeatureFlagRequestor,
                TimeSpan.FromSeconds(30),
                TimeSpan.Zero,
                testLogger
                );
        }

        [Fact]
        public void CanCreatePollingDataSource()
        {
            Assert.NotNull(MakeDataSource());
        }

        [Fact]
        public void StartWaitsUntilFlagCacheFilled()
        {
            var dataSource = MakeDataSource();
            var initTask = dataSource.Start();
            var unused = initTask.Wait(TimeSpan.FromSeconds(1));
            var flags = mockFlagCacheManager.FlagsForUser(user);
            Assert.Equal(3, flags.Count);
        }
    }
}
