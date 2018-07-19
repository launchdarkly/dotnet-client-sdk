using System;
using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class DefaultLdClientTests
    {
        static readonly string appKey = "some app key";

        LdClient Client()
        {
            User user = StubbedConfigAndUserBuilder.UserWithAllPropertiesFilledIn("user1Key");
            var configuration = TestUtil.ConfigWithFlagsJson(user, appKey, "{}");
            return TestUtil.CreateClient(configuration, user);
        }

        [Fact]
        public void CanCreateClientWithConfigAndUser()
        {
            Assert.NotNull(Client());
        }

        [Fact]
        public void CannotCreateClientWithNullConfig()
        {
            Assert.Throws<ArgumentNullException>(() => LdClient.Init((Configuration)null, User.WithKey("user")));
        }

        [Fact]
        public void CannotCreateClientWithNullUser()
        {
            Configuration config = TestUtil.ConfigWithFlagsJson(User.WithKey("dummy"), appKey, "{}");
            Assert.Throws<ArgumentNullException>(() => LdClient.Init(config, null));
        }

        [Fact]
        public void IdentifyUpdatesTheUser()
        {
            var client = Client();
            var updatedUser = User.WithKey("some new key");
            client.Identify(updatedUser);
            Assert.Equal(client.User, updatedUser);
        }

        [Fact]
        public void IdentifyWithNullUserThrowsException()
        {
            var client = Client();
            Assert.Throws<AggregateException>(() => client.Identify(null));
        }

        [Fact]
        public void IdentifyAsyncWithNullUserThrowsException()
        {
            var client = Client();
            Assert.ThrowsAsync<AggregateException>(async () => await client.IdentifyAsync(null));
            // note that exceptions thrown out of an async task are always wrapped in AggregateException
        }

        [Fact]
        public void SharedClientIsTheOnlyClientAvailable()
        {
            lock (TestUtil.ClientInstanceLock)
            {
                User user = StubbedConfigAndUserBuilder.UserWithAllPropertiesFilledIn("user1Key");
                var config = TestUtil.ConfigWithFlagsJson(user, appKey, "{}");
                var client = LdClient.Init(config, user);
                try
                {
                    Assert.ThrowsAsync<Exception>(async () => await LdClient.InitAsync(config, User.WithKey("otherUserKey")));
                }
                finally
                {
                    LdClient.Instance = null;
                }
            }
        }
        
        [Fact]
        public void ConnectionManagerShouldKnowIfOnlineOrNot()
        {
            var client = Client();
            var connMgr = client.Config.ConnectionManager as MockConnectionManager;
            connMgr.ConnectionChanged += (bool obj) => client.Online = obj;
            connMgr.Connect(true);
            Assert.False(client.IsOffline());
            connMgr.Connect(false);
            Assert.False(client.Online);
        }

        [Fact]
        public void ConnectionChangeShouldStopUpdateProcessor()
        {
            var client = Client();
            var connMgr = client.Config.ConnectionManager as MockConnectionManager;
            connMgr.ConnectionChanged += (bool obj) => client.Online = obj;
            connMgr.Connect(false);
            var mockUpdateProc = client.Config.MobileUpdateProcessor as MockPollingProcessor;
            Assert.False(mockUpdateProc.IsRunning);
        }

        [Fact]
        public void UserWithNullKeyWillHaveUniqueKeySet()
        {
            var userWithNullKey = User.WithKey(null);
            var config = TestUtil.ConfigWithFlagsJson(userWithNullKey, "someOtherAppKey", "{}");
            var client = TestUtil.CreateClient(config, userWithNullKey);
            Assert.Equal(MockDeviceInfo.key, client.User.Key);
        }

        [Fact]
        public void UserWithEmptyKeyWillHaveUniqueKeySet()
        {
            var userWithEmptyKey = User.WithKey("");
            var config = TestUtil.ConfigWithFlagsJson(userWithEmptyKey, "someOtherAppKey", "{}");
            var client = TestUtil.CreateClient(config, userWithEmptyKey);
            Assert.Equal(MockDeviceInfo.key, client.User.Key);
        }

        [Fact]
        public void IdentifyWithUserWithNullKeyUsesUniqueGeneratedKey()
        {
            var client = Client();
            client.Identify(User.WithKey("a new user's key"));
            var userWithNullKey = User.WithKey(null);
            client.Identify(userWithNullKey);
            Assert.Equal(MockDeviceInfo.key, client.User.Key);
        }

        [Fact]
        public void IdentifyWithUserWithEmptyKeyUsesUniqueGeneratedKey()
        {
            var client = Client();
            var userWithEmptyKey = User.WithKey("");
            client.Identify(userWithEmptyKey);
            Assert.Equal(MockDeviceInfo.key, client.User.Key);
        }

        [Fact]
        public void UpdatingKeylessUserWillGenerateNewUserWithSameValues()
        {
            var updatedUser = StubbedConfigAndUserBuilder.UserWithAllPropertiesFilledIn(String.Empty);
            var client = Client();
            var previousUser = client.User;
            client.Identify(updatedUser);
            Assert.NotEqual(updatedUser, previousUser);
            Assert.Equal(updatedUser.Avatar, previousUser.Avatar);
            Assert.Equal(updatedUser.Country, previousUser.Country);
            Assert.Equal(updatedUser.Email, previousUser.Email);
            Assert.Equal(updatedUser.FirstName, previousUser.FirstName);
            Assert.Equal(updatedUser.LastName, previousUser.LastName);
            Assert.Equal(updatedUser.Name, previousUser.Name);
            Assert.Equal(updatedUser.IpAddress, previousUser.IpAddress);
            Assert.Equal(updatedUser.SecondaryKey, previousUser.SecondaryKey);
            Assert.Equal(updatedUser.Custom["somePrivateAttr1"], previousUser.Custom["somePrivateAttr1"]);
            Assert.Equal(updatedUser.Custom["somePrivateAttr2"], previousUser.Custom["somePrivateAttr2"]);
        }

        [Fact]
        public void UpdatingKeylessUserSetsAnonymousToTrue()
        {
            var updatedUser = User.WithKey(null);
            var client = Client();
            var previousUser = client.User;
            client.Identify(updatedUser);
            Assert.True(client.User.Anonymous);
        }

        [Fact]
        public void CanRegisterListener()
        {
            var client = Client();
            var listenerMgr = client.Config.FeatureFlagListenerManager as FeatureFlagListenerManager;
            var listener = new TestListener();
            client.RegisterFeatureFlagListener("user1-flag", listener);
            listenerMgr.FlagWasUpdated("user1-flag", 7);
            Assert.Equal(7, listener.FeatureFlags["user1-flag"].ToObject<int>());
        }

        [Fact]
        public void UnregisterListenerUnregistersPassedInListenerForFlagKeyOnListenerManager()
        {
            var client = Client();
            var listenerMgr = client.Config.FeatureFlagListenerManager as FeatureFlagListenerManager;
            var listener = new TestListener();
            client.RegisterFeatureFlagListener("user2-flag", listener);
            listenerMgr.FlagWasUpdated("user2-flag", 7);
            Assert.Equal(7, listener.FeatureFlags["user2-flag"]);

            client.UnregisterFeatureFlagListener("user2-flag", listener);
            listenerMgr.FlagWasUpdated("user2-flag", 12);
            Assert.NotEqual(12, listener.FeatureFlags["user2-flag"]);
        }
    }
}
