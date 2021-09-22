﻿using System;
using System.Threading;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Client.Internal;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client
{
    public class LdClientTests : BaseTest
    {
        static readonly string appKey = "some app key";
        static readonly User simpleUser = User.WithKey("user-key");

        public LdClientTests(ITestOutputHelper testOutput) : base(testOutput) { }

        ConfigurationBuilder BaseConfig() =>
            TestUtil.TestConfig(appKey).Logging(testLogging);

        LdClient Client()
        {
            var configuration = BaseConfig().Build();
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
            var config = BaseConfig().Build();
            Assert.Throws<ArgumentNullException>(() => LdClient.Init(config, null, TimeSpan.Zero));
        }

        [Fact]
        public void CannotCreateClientWithNegativeWaitTime()
        {
            var config = BaseConfig().Build();
            Assert.Throws<ArgumentOutOfRangeException>(() => LdClient.Init(config, simpleUser, TimeSpan.FromMilliseconds(-2)));
        }

        [Fact]
        public void CanCreateClientWithInfiniteWaitTime()
        {
            var config = BaseConfig().Build();
            using (var client = LdClient.Init(config, simpleUser, System.Threading.Timeout.InfiniteTimeSpan)) { }
            TestUtil.ClearClient();
        }

        [Fact]
        public async void InitPassesUserToUpdateProcessorFactory()
        {
            MockPollingProcessor stub = new MockPollingProcessor("{}");
            User testUser = User.WithKey("new-user");

            var config = TestUtil.ConfigWithFlagsJson(testUser, appKey, "{}")
                .DataSource(stub.AsFactory())
                .Logging(testLogging)
                .Build();

            using (var client = await LdClient.InitAsync(config, testUser))
            {
                var actualUser = client.User; // may have been transformed e.g. to add device/OS properties
                Assert.Equal(testUser.Key, actualUser.Key);
                Assert.Equal(actualUser, stub.ReceivedUser);
            }
        }

        [Fact]
        public async void InitWithAutoGeneratedAnonUserPassesGeneratedUserToUpdateProcessorFactory()
        {
            MockPollingProcessor stub = new MockPollingProcessor("{}");
            User anonUserIn = User.Builder((String)null).Anonymous(true).Build();

            var config = TestUtil.ConfigWithFlagsJson(anonUserIn, appKey, "{}")
                .DataSource(stub.AsFactory())
                .Logging(testLogging)
                .Build();

            using (var client = await LdClient.InitAsync(config, anonUserIn))
            {
                Assert.NotSame(anonUserIn, stub.ReceivedUser);
                Assert.Equal(MockDeviceInfo.GeneratedId, stub.ReceivedUser.Key);
                Assert.True(stub.ReceivedUser.Anonymous);
            }
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
            var userAFlags = new DataSetBuilder()
                .Add(flagKey, 1, LdValue.Of("a-value"), 0).Build();
            var userBFlags = new DataSetBuilder()
                .Add(flagKey, 2, LdValue.Of("b-value"), 1).Build();

            var startedIdentifyUserB = new SemaphoreSlim(0, 1);
            var canFinishIdentifyUserB = new SemaphoreSlim(0, 1);
            var finishedIdentifyUserB = new SemaphoreSlim(0, 1);

            var dataSourceFactory = new MockDataSourceFactoryFromLambda((ctx, updates, user, bg) =>
                new MockDataSourceFromLambda(user, async () =>
                {
                    switch (user.Key)
                    {
                        case "a":
                            updates.Init(user, userAFlags);
                            break;

                        case "b":
                            startedIdentifyUserB.Release();
                            await canFinishIdentifyUserB.WaitAsync();
                            updates.Init(user, userBFlags);
                            break;
                    }
                }));

            var config = TestUtil.ConfigWithFlagsJson(userA, appKey, "{}")
                .DataSource(dataSourceFactory)
                .Logging(testLogging)
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
        public async void IdentifyPassesUserToDataSource()
        {
            MockPollingProcessor stub = new MockPollingProcessor("{}");
            User newUser = User.WithKey("new-user");

            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .DataSource(stub.AsFactory())
                .Logging(testLogging)
                .Build();

            using (var client = await LdClient.InitAsync(config, simpleUser))
            {
                var actualUser = client.User; // may have been transformed e.g. to add device/OS properties
                Assert.Equal(simpleUser.Key, actualUser.Key);
                Assert.Equal(actualUser, stub.ReceivedUser);

                await client.IdentifyAsync(newUser);

                actualUser = client.User;
                Assert.Equal(newUser.Key, actualUser.Key);
                Assert.Equal(actualUser, stub.ReceivedUser);
            }
        }

        [Fact]
        public async void IdentifyWithAutoGeneratedAnonUserPassesGeneratedUserToUpdateProcessorFactory()
        {
            MockPollingProcessor stub = new MockPollingProcessor("{}");
            User anonUserIn = User.Builder((String)null).Anonymous(true).Build();

            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .DataSource(stub.AsFactory())
                .Logging(testLogging)
                .Build();

            using (var client = await LdClient.InitAsync(config, simpleUser))
            {
                var actualUser = client.User; // may have been transformed e.g. to add device/OS properties
                Assert.Equal(simpleUser.Key, actualUser.Key);
                Assert.Equal(actualUser, stub.ReceivedUser);

                await client.IdentifyAsync(anonUserIn);

                Assert.NotSame(simpleUser, stub.ReceivedUser);
                Assert.Equal(MockDeviceInfo.GeneratedId, stub.ReceivedUser.Key);
                Assert.True(stub.ReceivedUser.Anonymous);
            }
        }

        [Fact]
        public void SharedClientIsTheOnlyClientAvailable()
        {
            TestUtil.WithClientLock(() =>
            {
                var config = BaseConfig().Build();
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
                var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}").Logging(testLogging).Build();
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
                .DataSource(mockUpdateProc.AsFactory())
                .ConnectivityStateManager(mockConnectivityStateManager)
                .Logging(testLogging)
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
                .DeviceInfo(new MockDeviceInfo(uniqueId))
                .Logging(testLogging)
                .Build();
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
                .DeviceInfo(new MockDeviceInfo(uniqueId))
                .Logging(testLogging)
                .Build();
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
                .DeviceInfo(new MockDeviceInfo(uniqueId))
                .Logging(testLogging)
                .Build();
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
                .DeviceInfo(new MockDeviceInfo(uniqueId))
                .Logging(testLogging)
                .Build();
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
                .Secondary("secondary")
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
                .DeviceInfo(new MockDeviceInfo(uniqueId))
                .Logging(testLogging)
                .Build();
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
                Assert.Equal(user.Secondary, newUser.Secondary);
                Assert.Equal(user.Custom["attr"], newUser.Custom["attr"]);
                Assert.True(newUser.Anonymous);
            }
        }
        
        [Fact]
        public void FlagsAreLoadedFromPersistentStorageByDefault()
        {
            var storage = new MockPersistentDataStore();
            var flags = new DataSetBuilder().Add("flag", 1, LdValue.Of(100), 0).Build();
            storage.Init(simpleUser, DataModelSerialization.SerializeAll(flags));
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .Persistence(new SinglePersistentDataStoreFactory(storage))
                .Offline(true)
                .Logging(testLogging)
                .Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                Assert.Equal(100, client.IntVariation("flag", 99));
            }
        }

        [Fact]
        public void FlagsAreSavedToPersistentStorageByDefault()
        {
            var storage = new MockPersistentDataStore();
            var initialFlags = new DataSetBuilder().Add("flag", 1, LdValue.Of(100), 0).Build();
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .DataSource(MockPollingProcessor.Factory(TestUtil.MakeJsonData(initialFlags)))
                .Persistence(new SinglePersistentDataStoreFactory(storage))
                .Logging(testLogging)
                .Build();
            using (var client = TestUtil.CreateClient(config, simpleUser))
            {
                var storedData = storage.GetAll(simpleUser);
                Assert.NotNull(storedData);
                var flags = DataModelSerialization.DeserializeAll(storedData);
                Assert.NotEmpty(flags.Items);
                Assert.Equal(100, flags.Items[0].Value.Item.Value.AsInt);
            }
        }

        [Fact]
        public void EventProcessorIsOnlineByDefault()
        {
            var eventProcessor = new MockEventProcessor();
            var config = TestUtil.ConfigWithFlagsJson(simpleUser, appKey, "{}")
                .Events(new SingleEventProcessorFactory(eventProcessor))
                .Logging(testLogging)
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
                .Events(new SingleEventProcessorFactory(eventProcessor))
                .Offline(true)
                .Logging(testLogging)
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
                .Events(new SingleEventProcessorFactory(eventProcessor))
                .Logging(testLogging)
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
