using System;
using System.Collections.Generic;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class DefaultLdClientTests
    {
        static readonly string appKey = "some app key";
        static readonly string flagKey = "some flag key";

        LdClient Client()
        {
            if (LdClient.Instance == null)
            {
                User user = StubbedConfigAndUserBuilder.UserWithAllPropertiesFilledIn("user1Key");
                var configuration = StubbedConfigAndUserBuilder.Config(user, appKey);
                return LdClient.Init(configuration, user);
            }

            return LdClient.Instance;
        }

        [Fact]
        public void CanCreateClientWithConfigAndUser()
        {
            Assert.NotNull(Client());
        }

        [Fact]
        public void DefaultBoolVariationFlag()
        {
            Assert.False(Client().BoolVariation(flagKey));
        }

        [Fact]
        public void DefaultStringVariationFlag()
        {
            Assert.Equal(String.Empty, Client().StringVariation(flagKey, String.Empty));
        }

        [Fact]
        public void DefaultFloatVariationFlag()
        {
            Assert.Equal(0, Client().FloatVariation(flagKey));
        }

        [Fact]
        public void DefaultIntVariationFlag()
        {
            Assert.Equal(0, Client().IntVariation(flagKey));
        }

        [Fact]
        public void DefaultJSONVariationFlag()
        {
            Assert.Null(Client().JsonVariation(flagKey, null));
        }

        [Fact]
        public void DefaultAllFlagsShouldBeEmpty()
        {
            var client = Client();
            client.Identify(User.WithKey("some other user key with no flags"));
            Assert.Equal(0, client.AllFlags().Count);
            client.Identify(User.WithKey("user1Key"));
        }

        [Fact]
        public void DefaultValueReturnedIfTypeBackIsDifferent()
        {
            var client = Client();
            Assert.Equal(0, client.IntVariation("string-flag", 0));
            Assert.False(client.BoolVariation("float-flag", false));
        }

        [Fact]
        public void DefaultValueReturnedIfFlagIsOff()
        {
            var client = Client();
            Assert.Equal(123, client.IntVariation("off-flag", 123));
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
        public void SharedClientIsTheOnlyClientAvailable()
        {
            var client = Client();
            var config = Configuration.Default(appKey);
            Assert.ThrowsAsync<Exception>(async () => await LdClient.InitAsync(config, User.WithKey("otherUserKey")));
        }

        [Fact]
        public void CanFetchFlagFromInMemoryCache()
        {
            var client = Client();
            bool boolFlag = client.BoolVariation("boolean-flag", true);
            Assert.True(boolFlag);
            int intFlag = client.IntVariation("int-flag", 0);
            Assert.Equal(15, intFlag);
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
            LdClient.Instance = null;
            var userWithNullKey = User.WithKey(null);
            var config = StubbedConfigAndUserBuilder.Config(userWithNullKey, "someOtherAppKey");
            var client = LdClient.Init(config, userWithNullKey);
            Assert.Equal(MockDeviceInfo.key, client.User.Key);
            LdClient.Instance = null;
        }

        [Fact]
        public void IdentifyWithUserMissingKeyUsesUniqueGeneratedKey()
        {
            var client = Client();
            LdClient.Instance.Identify(User.WithKey("a new user's key"));
            var userWithNullKey = User.WithKey(null);
            LdClient.Instance.Identify(userWithNullKey);
            Assert.Equal(MockDeviceInfo.key, LdClient.Instance.User.Key);
            LdClient.Instance = null;
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
