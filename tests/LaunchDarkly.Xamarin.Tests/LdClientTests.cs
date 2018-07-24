using System;
using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class DefaultLdClientTests
    {
        static readonly string appKey = "some app key";
        static readonly User simpleUser = User.WithKey("user-key");

        LdClient Client()
        {
            var configuration = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}");
            return TestUtil.CreateClient(configuration, simpleUser);
        }
        
        [Fact]
        public void CannotCreateClientWithNullConfig()
        {
            Assert.Throws<ArgumentNullException>(() => LdClient.Init((Configuration)null, simpleUser, TimeSpan.Zero));
        }

        [Fact]
        public void CannotCreateClientWithNullUser()
        {
            Configuration config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}");
            Assert.Throws<ArgumentNullException>(() => LdClient.Init(config, null, TimeSpan.Zero));
        }

        [Fact]
        public void IdentifyUpdatesTheUser()
        {
            using (var client = Client())
            {
                var updatedUser = User.WithKey("some new key");
                client.Identify(updatedUser);
                Assert.Equal(client.User, updatedUser);
            }
        }

        [Fact]
        public void IdentifyWithNullUserThrowsException()
        {
            using (var client = Client())
            {
                Assert.Throws<ArgumentNullException>(() => client.Identify(null));
            }
        }

        [Fact]
        public void IdentifyAsyncWithNullUserThrowsException()
        {
            using (var client = Client())
            {
                Assert.ThrowsAsync<AggregateException>(async () => await client.IdentifyAsync(null));
                // note that exceptions thrown out of an async task are always wrapped in AggregateException
            }
        }

        [Fact]
        public void SharedClientIsTheOnlyClientAvailable()
        {
            lock (TestUtil.ClientInstanceLock)
            {
                var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}");
                using (var client = LdClient.Init(config, simpleUser, TimeSpan.Zero))
                {
                    try
                    {
                        Assert.ThrowsAsync<Exception>(async () => await LdClient.InitAsync(config, simpleUser));
                    }
                    finally
                    {
                        LdClient.Instance = null;
                    }
                }
            }
        }
        
        [Fact]
        public void ConnectionManagerShouldKnowIfOnlineOrNot()
        {
            using (var client = Client())
            {
                var connMgr = client.Config.ConnectionManager as MockConnectionManager;
                connMgr.ConnectionChanged += (bool obj) => client.Online = obj;
                connMgr.Connect(true);
                Assert.False(client.IsOffline());
                connMgr.Connect(false);
                Assert.False(client.Online);
            }
        }

        [Fact]
        public void ConnectionChangeShouldStopUpdateProcessor()
        {
            using (var client = Client())
            {
                var connMgr = client.Config.ConnectionManager as MockConnectionManager;
                connMgr.ConnectionChanged += (bool obj) => client.Online = obj;
                connMgr.Connect(false);
                var mockUpdateProc = client.Config.MobileUpdateProcessor as MockPollingProcessor;
                Assert.False(mockUpdateProc.IsRunning);
            }
        }

        [Fact]
        public void UserWithNullKeyWillHaveUniqueKeySet()
        {
            var userWithNullKey = User.WithKey(null);
            var uniqueId = "some-unique-key";
            var config = TestUtil.ConfigWithFlagsJson(userWithNullKey, appKey, "{}")
                .WithDeviceInfo(new MockDeviceInfo(uniqueId));
            using (var client = TestUtil.CreateClient(config, userWithNullKey))
            {
                Assert.Equal(uniqueId, client.User.Key);
                Assert.True(client.User.Anonymous);
            }
        }

        [Fact]
        public void UserWithEmptyKeyWillHaveUniqueKeySet()
        {
            var userWithEmptyKey = User.WithKey("");
            var uniqueId = "some-unique-key";
            var config = TestUtil.ConfigWithFlagsJson(userWithEmptyKey, appKey, "{}")
                .WithDeviceInfo(new MockDeviceInfo(uniqueId));
            using (var client = TestUtil.CreateClient(config, userWithEmptyKey))
            {
                Assert.Equal(uniqueId, client.User.Key);
                Assert.True(client.User.Anonymous);
            }
        }

        [Fact]
        public void IdentifyWithUserWithNullKeyUsesUniqueGeneratedKey()
        {
            var userWithNullKey = User.WithKey(null);
            var uniqueId = "some-unique-key";
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .WithDeviceInfo(new MockDeviceInfo(uniqueId));
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                client.Identify(userWithNullKey);
                Assert.Equal(uniqueId, client.User.Key);
                Assert.True(client.User.Anonymous);
            }
        }

        [Fact]
        public void IdentifyWithUserWithEmptyKeyUsesUniqueGeneratedKey()
        {
            var userWithEmptyKey = User.WithKey("");
            var uniqueId = "some-unique-key";
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .WithDeviceInfo(new MockDeviceInfo(uniqueId));
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                client.Identify(userWithEmptyKey);
                Assert.Equal(uniqueId, client.User.Key);
                Assert.True(client.User.Anonymous);
            }
        }

        [Fact]
        public void AllOtherAttributesArePreservedWhenSubstitutingUniqueUserKey()
        {
            var user = User.WithKey("")
                .AndSecondaryKey("secondary")
                .AndIpAddress("10.0.0.1")
                .AndCountry("US")
                .AndFirstName("John")
                .AndLastName("Doe")
                .AndName("John Doe")
                .AndAvatar("images.google.com/myAvatar")
                .AndEmail("test@example.com")
                .AndCustomAttribute("attr", "value");
            var uniqueId = "some-unique-key";
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .WithDeviceInfo(new MockDeviceInfo(uniqueId));
            using (var client = TestUtil.CreateClient(config, simpleUser))
            { 
                client.Identify(user);
                User newUser = client.User;
                Assert.NotEqual(user.Key, newUser.Key);
                Assert.Equal(user.Avatar, newUser.Avatar);
                Assert.Equal(user.Country, newUser.Country);
                Assert.Equal(user.Email, newUser.Email);
                Assert.Equal(user.FirstName, newUser.FirstName);
                Assert.Equal(user.LastName, newUser.LastName);
                Assert.Equal(user.Name, newUser.Name);
                Assert.Equal(user.IpAddress, newUser.IpAddress);
                Assert.Equal(user.SecondaryKey, newUser.SecondaryKey);
                Assert.Equal(user.Custom["attr"], newUser.Custom["attr"]);
                Assert.True(newUser.Anonymous);
            }
        }
        
        [Fact]
        public void CanRegisterListener()
        {
            using (var client = Client())
            {
                var listenerMgr = client.Config.FeatureFlagListenerManager as FeatureFlagListenerManager;
                var listener = new TestListener();
                client.RegisterFeatureFlagListener("user1-flag", listener);
                listenerMgr.FlagWasUpdated("user1-flag", 7);
                Assert.Equal(7, listener.FeatureFlags["user1-flag"].ToObject<int>());
            }
        }

        [Fact]
        public void UnregisterListenerUnregistersPassedInListenerForFlagKeyOnListenerManager()
        {
            using (var client = Client())
            {
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
}
