using LaunchDarkly.Client;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class FlagCacheManagerTests : BaseTest
    {
        private const string initialFlagsJson = "{" +
            "\"int-flag\":{\"value\":15}," +
            "\"float-flag\":{\"value\":13.5}," +
            "\"string-flag\":{\"value\":\"markw@magenic.com\"}" +
            "}";

        IUserFlagCache deviceCache = new UserFlagInMemoryCache();
        IUserFlagCache inMemoryCache = new UserFlagInMemoryCache();
        FlagChangedEventManager listenerManager = new FlagChangedEventManager();

        User user = User.WithKey("someKey");

        IFlagCacheManager ManagerWithCachedFlags()
        {
            var flagCacheManager = new FlagCacheManager(deviceCache, inMemoryCache, listenerManager, user);
            var flags = TestUtil.DecodeFlagsJson(initialFlagsJson);
            flagCacheManager.CacheFlagsFromService(flags, user);
            return flagCacheManager;
        }

        [Fact]
        public void CacheFlagsShouldStoreFlagsInDeviceCache()
        {
            var flagCacheManager = ManagerWithCachedFlags();
            var cachedDeviceFlags = deviceCache.RetrieveFlags(user);
            Assert.Equal(15, cachedDeviceFlags["int-flag"].value.ToObject<int>());
            Assert.Equal("markw@magenic.com", cachedDeviceFlags["string-flag"].value.ToString());
            Assert.Equal(13.5, cachedDeviceFlags["float-flag"].value.ToObject<double>());
        }

        [Fact]
        public void CacheFlagsShouldAlsoStoreFlagsInMemoryCache()
        {
            var flagCacheManager = ManagerWithCachedFlags();
            var cachedDeviceFlags = inMemoryCache.RetrieveFlags(user);
            Assert.Equal(15, cachedDeviceFlags["int-flag"].value.ToObject<int>());
            Assert.Equal("markw@magenic.com", cachedDeviceFlags["string-flag"].value.ToString());
            Assert.Equal(13.5, cachedDeviceFlags["float-flag"].value.ToObject<double>());
        }

        [Fact]
        public void CanRemoveFlagForUser()
        {
            var manager = ManagerWithCachedFlags();
            manager.RemoveFlagForUser("int-key", user);
            Assert.Null(manager.FlagForUser("int-key", user));
        }

        [Fact]
        public void CanUpdateFlagForUser()
        {
            var flagCacheManager = ManagerWithCachedFlags();
            var updatedFeatureFlag = new FeatureFlag();
            updatedFeatureFlag.value = JToken.FromObject(5);
            updatedFeatureFlag.version = 12;
            flagCacheManager.UpdateFlagForUser("int-flag", updatedFeatureFlag, user);
            var updatedFlagFromCache = flagCacheManager.FlagForUser("int-flag", user);
            Assert.Equal(5, updatedFlagFromCache.value.ToObject<int>());
            Assert.Equal(12, updatedFeatureFlag.version);
        }

        [Fact]
        public void UpdateFlagSendsFlagChangeEvent()
        {
            var listener = new FlagChangedEventSink();
            listenerManager.FlagChanged += listener.Handler;

            var flagCacheManager = ManagerWithCachedFlags();
            var updatedFeatureFlag = new FeatureFlag();
            updatedFeatureFlag.value = JToken.FromObject(7);

            flagCacheManager.UpdateFlagForUser("int-flag", updatedFeatureFlag, user);

            var e = listener.Await();
            Assert.Equal("int-flag", e.Key);
            Assert.Equal(7, e.NewValue.AsInt);
            Assert.False(e.FlagWasDeleted);
        }

        [Fact]
        public void RemoveFlagSendsFlagChangeEvent()
        {
            var listener = new FlagChangedEventSink();
            listenerManager.FlagChanged += listener.Handler;

            var flagCacheManager = ManagerWithCachedFlags();
            var updatedFeatureFlag = new FeatureFlag();
            updatedFeatureFlag.value = JToken.FromObject(7);
            flagCacheManager.RemoveFlagForUser("int-flag", user);

            var e = listener.Await();
            Assert.Equal("int-flag", e.Key);
            Assert.True(e.FlagWasDeleted);
        }

        [Fact]
        public void CacheFlagsFromServiceUpdatesListenersIfFlagValueChanged()
        {
            var listener = new FlagChangedEventSink();
            listenerManager.FlagChanged += listener.Handler;

            var flagCacheManager = ManagerWithCachedFlags();
            var newFlagsJson = "{\"int-flag\":{\"value\":5}}";
            flagCacheManager.CacheFlagsFromService(TestUtil.DecodeFlagsJson(newFlagsJson), user);

            var e = listener.Await();
            Assert.Equal("int-flag", e.Key);
            Assert.Equal(5, e.NewValue.AsInt);
            Assert.False(e.FlagWasDeleted);
        }
    }
}
