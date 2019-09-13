using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class LdClientTests : BaseTest
    {
        static readonly string appKey = "some app key";
        static readonly User simpleUser = User.WithKey("user-key");

        LdClient Client()
        {
            var configuration = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}").Build();
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
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}").Build();
            Assert.Throws<ArgumentNullException>(() => LdClient.Init(config, null, TimeSpan.Zero));
        }

        [Fact]
        public void CannotCreateClientWithNegativeWaitTime()
        {
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}").Build();
            Assert.Throws<ArgumentOutOfRangeException>(() => LdClient.Init(config, simpleUser, TimeSpan.FromMilliseconds(-2)));
        }

        [Fact]
        public void CanCreateClientWithInfiniteWaitTime()
        {
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}").Build();
            using (var client = LdClient.Init(config, simpleUser, System.Threading.Timeout.InfiniteTimeSpan)) { }
            TestUtil.ClearClient();
        }

        [Fact]
        public void IdentifyUpdatesTheUser()
        {
            using (var client = Client())
            {
                var updatedUser = User.WithKey("some new key");
                var success = client.Identify(updatedUser, TimeSpan.FromSeconds(1));
                Assert.True(success);
                Assert.Equal(client.User.Key, updatedUser.Key); // don't compare entire user, because SDK may have added device/os attributes
            }
        }

        [Fact]
        public Task IdentifyAsyncCompletesOnlyWhenNewFlagsAreAvailable()
            => IdentifyCompletesOnlyWhenNewFlagsAreAvailable((client, user) => client.IdentifyAsync(user));

        [Fact]
        public Task IdentifySyncCompletesOnlyWhenNewFlagsAreAvailable()
            => IdentifyCompletesOnlyWhenNewFlagsAreAvailable((client, user) => Task.Run(() => client.Identify(user, TimeSpan.FromSeconds(4))));

        private async Task IdentifyCompletesOnlyWhenNewFlagsAreAvailable(Func<LdClient, User, Task> identifyTask)
        {
            var userA = User.WithKey("a");
            var userB = User.WithKey("b");

            var flagKey = "flag";
            var userAFlags = TestUtil.MakeSingleFlagData(flagKey, LdValue.Of("a-value"));
            var userBFlags = TestUtil.MakeSingleFlagData(flagKey, LdValue.Of("b-value"));

            var startedIdentifyUserB = new SemaphoreSlim(0, 1);
            var canFinishIdentifyUserB = new SemaphoreSlim(0, 1);
            var finishedIdentifyUserB = new SemaphoreSlim(0, 1);

            Func<Configuration, IFlagCacheManager, User, IMobileUpdateProcessor> updateProcessorFactory = (c, flags, user) =>
                new MockUpdateProcessorFromLambda(user, async () =>
                {
                    switch (user.Key)
                    {
                        case "a":
                            flags.CacheFlagsFromService(userAFlags, user);
                            break;

                        case "b":
                            startedIdentifyUserB.Release();
                            await canFinishIdentifyUserB.WaitAsync();
                            flags.CacheFlagsFromService(userBFlags, user);
                            break;
                    }
                });

            var config = TestUtil.ConfigWithFlagsJson(userA, appKey, "{}")
                .UpdateProcessorFactory(updateProcessorFactory)
                .Build();

            ClearCachedFlags(userA);
            ClearCachedFlags(userB);

            using (var client = await LdClient.InitAsync(config, userA))
            {
                Assert.True(client.Initialized);
                Assert.Equal("a-value", client.StringVariation(flagKey, null));

                var identifyUserBTask = Task.Run(async () =>
                {
                    await identifyTask(client, userB);
                    finishedIdentifyUserB.Release();
                });

                await startedIdentifyUserB.WaitAsync();

                Assert.False(client.Initialized);
                Assert.Null(client.StringVariation(flagKey, null));

                canFinishIdentifyUserB.Release();
                await finishedIdentifyUserB.WaitAsync();

                Assert.True(client.Initialized);
                Assert.Equal("b-value", client.StringVariation(flagKey, null));
            }
        }

        [Fact]
        public void IdentifyWithNullUserThrowsException()
        {
            using (var client = Client())
            {
                Assert.Throws<ArgumentNullException>(() => client.Identify(null, TimeSpan.Zero));
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
            TestUtil.WithClientLock(() =>
            {
                var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}").Build();
                using (var client = LdClient.Init(config, simpleUser, TimeSpan.Zero))
                {
                    Assert.Throws<Exception>(() => LdClient.Init(config, simpleUser, TimeSpan.Zero));
                }
                TestUtil.ClearClient();
            });
        }

        [Fact]
        public void CanCreateNewClientAfterDisposingOfSharedInstance()
        {
            TestUtil.WithClientLock(() =>
            {
                TestUtil.ClearClient();
                var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}").Build();
                using (var client0 = LdClient.Init(config, simpleUser, TimeSpan.Zero)) { }
                Assert.Null(LdClient.Instance);
                // Dispose() is called automatically at end of "using" block
                using (var client1 = LdClient.Init(config, simpleUser, TimeSpan.Zero)) { }
            });
        }

        [Fact]
        public void ConnectionChangeShouldStopUpdateProcessor()
        {
            var mockUpdateProc = new MockPollingProcessor(null);
            var mockConnectivityStateManager = new MockConnectivityStateManager(true);
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .UpdateProcessorFactory(mockUpdateProc.AsFactory())
                .ConnectivityStateManager(mockConnectivityStateManager)
                .Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                mockConnectivityStateManager.Connect(false);
                Assert.False(mockUpdateProc.IsRunning);
            }
        }

        [Fact]
        public void UserWithNullKeyWillHaveUniqueKeySet()
        {
            var userWithNullKey = User.WithKey(null);
            var uniqueId = "some-unique-key";
            var config = TestUtil.ConfigWithFlagsJson(userWithNullKey, appKey, "{}")
                .DeviceInfo(new MockDeviceInfo(uniqueId)).Build();
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
                .DeviceInfo(new MockDeviceInfo(uniqueId)).Build();
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
                .DeviceInfo(new MockDeviceInfo(uniqueId)).Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                client.Identify(userWithNullKey, TimeSpan.FromSeconds(1));
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
                .DeviceInfo(new MockDeviceInfo(uniqueId)).Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                client.Identify(userWithEmptyKey, TimeSpan.FromSeconds(1));
                Assert.Equal(uniqueId, client.User.Key);
                Assert.True(client.User.Anonymous);
            }
        }

        [Fact]
        public void AllOtherAttributesArePreservedWhenSubstitutingUniqueUserKey()
        {
            var user = User.Builder("")
                .SecondaryKey("secondary")
                .IPAddress("10.0.0.1")
                .Country("US")
                .FirstName("John")
                .LastName("Doe")
                .Name("John Doe")
                .Avatar("images.google.com/myAvatar")
                .Email("test@example.com")
                .Custom("attr", "value")
                .Build();
            var uniqueId = "some-unique-key";
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .DeviceInfo(new MockDeviceInfo(uniqueId)).Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            { 
                client.Identify(user, TimeSpan.FromSeconds(1));
                User newUser = client.User;
                Assert.NotEqual(user.Key, newUser.Key);
                Assert.Equal(user.Avatar, newUser.Avatar);
                Assert.Equal(user.Country, newUser.Country);
                Assert.Equal(user.Email, newUser.Email);
                Assert.Equal(user.FirstName, newUser.FirstName);
                Assert.Equal(user.LastName, newUser.LastName);
                Assert.Equal(user.Name, newUser.Name);
                Assert.Equal(user.IPAddress, newUser.IPAddress);
                Assert.Equal(user.SecondaryKey, newUser.SecondaryKey);
                Assert.Equal(user.Custom["attr"], newUser.Custom["attr"]);
                Assert.True(newUser.Anonymous);
            }
        }
        
        [Fact]
        public void CanRegisterAndUnregisterFlagChangedHandlers()
        {
            using (var client = Client())
            {
                EventHandler<FlagChangedEventArgs> handler1 = (sender, args) => { };
                EventHandler<FlagChangedEventArgs> handler2 = (sender, args) => { };
                var eventManager = client.flagChangedEventManager as FlagChangedEventManager;
                client.FlagChanged += handler1;
                client.FlagChanged += handler2;
                client.FlagChanged -= handler1;
                Assert.False(eventManager.IsHandlerRegistered(handler1));
                Assert.True(eventManager.IsHandlerRegistered(handler2));
            }
        }

        [Fact]
        public void FlagsAreLoadedFromPersistentStorageByDefault()
        {
            var storage = new MockPersistentStorage();
            var flagsJson = "{\"flag\": {\"value\": 100}}";
            storage.Save(Constants.FLAGS_KEY_PREFIX + simpleUser.Key, flagsJson);
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .PersistentStorage(storage)
                .FlagCacheManager(null) // use actual cache logic, not mock component (even though persistence layer is a mock)
                .Offline(true)
                .Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                Assert.Equal(100, client.IntVariation("flag", 99));
            }
        }

        [Fact]
        public void FlagsAreNotLoadedFromPersistentStorageIfPersistFlagValuesIsFalse()
        {
            var storage = new MockPersistentStorage();
            var flagsJson = "{\"flag\": {\"value\": 100}}";
            storage.Save(Constants.FLAGS_KEY_PREFIX + simpleUser.Key, flagsJson);
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .PersistentStorage(storage)
                .FlagCacheManager(null) // use actual cache logic, not mock component (even though persistence layer is a mock)
                .PersistFlagValues(false)
                .Offline(true)
                .Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                Assert.Equal(99, client.IntVariation("flag", 99)); // returns default value
            }
        }

        [Fact]
        public void FlagsAreSavedToPersistentStorageByDefault()
        {
            var storage = new MockPersistentStorage();
            var flagsJson = "{\"flag\": {\"value\": 100}}";
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, flagsJson)
                .FlagCacheManager(null)
                .UpdateProcessorFactory(MockPollingProcessor.Factory(flagsJson))
                .PersistentStorage(storage)
                .Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                var storedJson = storage.GetValue(Constants.FLAGS_KEY_PREFIX + simpleUser.Key);
                var flags = JsonConvert.DeserializeObject<IDictionary<string, FeatureFlag>>(storedJson);
                Assert.Equal(100, flags["flag"].value.AsInt);
            }
        }

        [Fact]
        public void EventProcessorIsOnlineByDefault()
        {
            var eventProcessor = new MockEventProcessor();
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .EventProcessor(eventProcessor)
                .Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                Assert.False(eventProcessor.Offline);
            }
        }

        [Fact]
        public void EventProcessorIsOfflineWhenClientIsConfiguredOffline()
        {
            var connectivityStateManager = new MockConnectivityStateManager(true);
            var eventProcessor = new MockEventProcessor();
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .ConnectivityStateManager(connectivityStateManager)
                .EventProcessor(eventProcessor)
                .Offline(true)
                .Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                Assert.True(eventProcessor.Offline);

                client.SetOffline(false, TimeSpan.FromSeconds(1));
                Assert.False(eventProcessor.Offline);

                client.SetOffline(true, TimeSpan.FromSeconds(1));
                Assert.True(eventProcessor.Offline);

                // If the network is unavailable...
                connectivityStateManager.Connect(false);

                // ...then even if Offline is set to false, events stay off
                client.SetOffline(false, TimeSpan.FromSeconds(1));
                Assert.True(eventProcessor.Offline);
            }
        }

        [Fact]
        public void EventProcessorIsOfflineWhenNetworkIsUnavailable()
        {
            var connectivityStateManager = new MockConnectivityStateManager(false);
            var eventProcessor = new MockEventProcessor();
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .ConnectivityStateManager(connectivityStateManager)
                .EventProcessor(eventProcessor)
                .Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                Assert.True(eventProcessor.Offline);

                connectivityStateManager.Connect(true);
                Assert.False(eventProcessor.Offline);

                connectivityStateManager.Connect(false);
                Assert.True(eventProcessor.Offline);

                // If client is configured offline...
                client.SetOffline(true, TimeSpan.FromSeconds(1));

                // ...then even if the network comes back on, events stay off
                connectivityStateManager.Connect(true);
                Assert.True(eventProcessor.Offline);
            }
        }
    }
}
