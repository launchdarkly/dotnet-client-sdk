using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client
{
    public class LdClientTests : BaseTest
    {
        private static readonly User KeylessAnonUser =
            User.Builder((string)null)
                .Anonymous(true)
                .Email("example").AsPrivateAttribute() // give it some more attributes so we can verify they are preserved
                .Custom("other", 3)
                .Build();

        public LdClientTests(ITestOutputHelper testOutput) : base(testOutput) { }

        [Fact]
        public void CannotCreateClientWithNullConfig()
        {
            Assert.Throws<ArgumentNullException>(() => LdClient.Init((Configuration)null, BasicUser, TimeSpan.Zero));
        }

        [Fact]
        public void CannotCreateClientWithNullUser()
        {
            var config = BasicConfig().Build();
            Assert.Throws<ArgumentNullException>(() => LdClient.Init(config, null, TimeSpan.Zero));
        }

        [Fact]
        public void CannotCreateClientWithNegativeWaitTime()
        {
            var config = BasicConfig().Build();
            Assert.Throws<ArgumentOutOfRangeException>(() => LdClient.Init(config, BasicUser, TimeSpan.FromMilliseconds(-2)));
        }

        [Fact]
        public void CanCreateClientWithInfiniteWaitTime()
        {
            var config = BasicConfig().Build();
            using (var client = LdClient.Init(config, BasicUser, System.Threading.Timeout.InfiniteTimeSpan)) { }
            TestUtil.ClearClient();
        }

        [Fact]
        public async void InitPassesUserToDataSource()
        {
            MockPollingProcessor stub = new MockPollingProcessor(DataSetBuilder.Empty);

            var config = BasicConfig()
                .DataSource(stub.AsFactory())
                .Build();

            using (var client = await LdClient.InitAsync(config, BasicUser))
            {
                var actualUser = client.User; // may have been transformed e.g. to add device/OS properties
                Assert.Equal(BasicUser.Key, actualUser.Key);
                Assert.Equal(actualUser, stub.ReceivedUser);
            }
        }

        [Fact]
        public async Task InitWithKeylessAnonUserAddsKey()
        {
            // Note, we don't care about polling mode vs. streaming mode for this functionality.
            var mockDeviceInfo = new MockDeviceInfo("fake-device-id");
            var config = BasicConfig().DeviceInfo(mockDeviceInfo).Persistence(Components.NoPersistence).Build();

            using (var client = await TestUtil.CreateClientAsync(config, KeylessAnonUser))
            {
                Assert.Equal("fake-device-id", client.User.Key);
                AssertHelpers.UsersEqualExcludingAutoProperties(
                    User.Builder(KeylessAnonUser).Key(client.User.Key).Build(),
                    client.User);
            }
        }

#if __MOBILE__
        [Fact]
        public async Task InitWithKeylessAnonUserUsesStableDeviceIDOnMobilePlatforms()
        {
            var config = BasicConfig().Persistence(Components.NoPersistence).Build();

            string generatedKey = null;
            using (var client = await TestUtil.CreateClientAsync(config, KeylessAnonUser))
            {
                generatedKey = client.User.Key;
                Assert.NotNull(generatedKey);
                AssertHelpers.UsersEqualExcludingAutoProperties(
                    User.Builder(KeylessAnonUser).Key(client.User.Key).Build(),
                    client.User);
            }

            using (var client = await TestUtil.CreateClientAsync(config, KeylessAnonUser))
            {
                Assert.Equal(generatedKey, client.User.Key);
            }
        }
#endif

#if !__MOBILE__
        [Fact]
        public async Task InitWithKeylessAnonUserGeneratesRandomizedIdOnNonMobilePlatforms()
        {
            var config = BasicConfig()
                .Persistence(Components.Persistence().Storage(new MockPersistentDataStore().AsSingletonFactory()))
                .Build();

            string generatedKey = null;
            using (var client = await TestUtil.CreateClientAsync(config, KeylessAnonUser))
            {
                generatedKey = client.User.Key;
                Assert.NotNull(generatedKey);
                AssertHelpers.UsersEqualExcludingAutoProperties(
                    User.Builder(KeylessAnonUser).Key(client.User.Key).Build(),
                    client.User);
            }

            using (var client = await TestUtil.CreateClientAsync(config, KeylessAnonUser))
            {
                Assert.Equal(generatedKey, client.User.Key);
            }

            // Now use a configuration where persistence is disabled - a different key is generated
            var configWithoutPersistence = BasicConfig().Persistence(Components.NoPersistence).Build();
            using (var client = await TestUtil.CreateClientAsync(configWithoutPersistence, KeylessAnonUser))
            {
                Assert.NotNull(client.User.Key);
                Assert.NotEqual(generatedKey, client.User.Key);
            }
        }
#endif

        [Fact]
        public async void InitWithKeylessAnonUserPassesGeneratedUserToDataSource()
        {
            MockPollingProcessor stub = new MockPollingProcessor(DataSetBuilder.Empty);

            var config = BasicConfig()
                .DataSource(stub.AsFactory())
                .Build();

            using (var client = await LdClient.InitAsync(config, KeylessAnonUser))
            {
                AssertHelpers.UsersEqualExcludingAutoProperties(
                    User.Builder(KeylessAnonUser).Key(stub.ReceivedUser.Key).Build(),
                    stub.ReceivedUser);
            }
        }

        [Fact]
        public void IdentifyUpdatesTheUser()
        {
            using (var client = TestUtil.CreateClient(BasicConfig().Build(), BasicUser))
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

            var config = BasicConfig()
                .DataSource(dataSourceFactory)
                .Build();

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
            using (var client = TestUtil.CreateClient(BasicConfig().Build(), BasicUser))
            {
                Assert.Throws<ArgumentNullException>(() => client.Identify(null, TimeSpan.Zero));
            }
        }

        [Fact]
        public void IdentifyAsyncWithNullUserThrowsException()
        {
            using (var client = TestUtil.CreateClient(BasicConfig().Build(), BasicUser))
            {
                Assert.ThrowsAsync<AggregateException>(async () => await client.IdentifyAsync(null));
                // note that exceptions thrown out of an async task are always wrapped in AggregateException
            }
        }

        [Fact]
        public async void IdentifyPassesUserToDataSource()
        {
            MockPollingProcessor stub = new MockPollingProcessor(DataSetBuilder.Empty);
            User newUser = User.WithKey("new-user");

            var config = BasicConfig()
                .DataSource(stub.AsFactory())
                .Build();

            using (var client = await LdClient.InitAsync(config, BasicUser))
            {
                AssertHelpers.UsersEqualExcludingAutoProperties(BasicUser, client.User);
                Assert.Equal(client.User, stub.ReceivedUser);

                await client.IdentifyAsync(newUser);

                AssertHelpers.UsersEqualExcludingAutoProperties(newUser, client.User);
                Assert.Equal(client.User, stub.ReceivedUser);
            }
        }

        [Fact]
        public async Task IdentifyWithKeylessAnonUserAddsKey()
        {
            var mockDeviceInfo = new MockDeviceInfo("fake-device-id");
            var config = BasicConfig().DeviceInfo(mockDeviceInfo).Persistence(Components.NoPersistence).Build();

            using (var client = await TestUtil.CreateClientAsync(config, BasicUser))
            {
                await client.IdentifyAsync(KeylessAnonUser);

                Assert.Equal("fake-device-id", client.User.Key);
                AssertHelpers.UsersEqualExcludingAutoProperties(
                    User.Builder(KeylessAnonUser).Key(client.User.Key).Build(),
                    client.User);
            }
        }

#if __MOBILE__
        [Fact]
        public async Task IdentifyWithKeylessAnonUserUsesStableDeviceIDOnMobilePlatforms()
        {
            var config = BasicConfig().Persistence(Components.NoPersistence).Build();
            
            string generatedKey = null;
            using (var client = await TestUtil.CreateClientAsync(config, BasicUser))
            {
                await client.IdentifyAsync(KeylessAnonUser);

                generatedKey = client.User.Key;
                Assert.NotNull(generatedKey);
                AssertHelpers.UsersEqualExcludingAutoProperties(
                    User.Builder(KeylessAnonUser).Key(client.User.Key).Build(),
                    client.User);
            }

            using (var client = await TestUtil.CreateClientAsync(config, BasicUser))
            {
                client.Identify(KeylessAnonUser, TimeSpan.FromSeconds(1));

                Assert.Equal(generatedKey, client.User.Key);
            }
        }
#endif

#if !__MOBILE__
        [Fact]
        public async Task IdentifyWithKeylessAnonUserGeneratesRandomizedIdOnNonMobilePlatforms()
        {
            var config = BasicConfig()
                .Persistence(Components.Persistence().Storage(new MockPersistentDataStore().AsSingletonFactory()))
                .Build();

            string generatedKey = null;
            using (var client = await TestUtil.CreateClientAsync(config, BasicUser))
            {
                await client.IdentifyAsync(KeylessAnonUser);

                generatedKey = client.User.Key;
                Assert.NotNull(generatedKey);
                AssertHelpers.UsersEqualExcludingAutoProperties(
                    User.Builder(KeylessAnonUser).Key(client.User.Key).Build(),
                    client.User);
             }

            using (var client = await TestUtil.CreateClientAsync(config, BasicUser))
            {
                await client.IdentifyAsync(KeylessAnonUser);

                Assert.Equal(generatedKey, client.User.Key);
            }

            // Now use a configuration where persistence is disabled - a different key is generated
            var configWithoutPersistence = BasicConfig().Persistence(Components.NoPersistence).Build();
            using (var client = await TestUtil.CreateClientAsync(configWithoutPersistence, BasicUser))
            {
                await client.IdentifyAsync(KeylessAnonUser);

                Assert.NotNull(client.User.Key);
                Assert.NotEqual(generatedKey, client.User.Key);
            }
        }
#endif

        [Fact]
        public async void IdentifyWithKeylessAnonUserPassesGeneratedUserToDataSource()
        {
            MockPollingProcessor stub = new MockPollingProcessor(DataSetBuilder.Empty);

            var config = BasicConfig()
                .DataSource(stub.AsFactory())
                .DeviceInfo(new MockDeviceInfo())
                .Build();

            using (var client = await LdClient.InitAsync(config, BasicUser))
            {
                await client.IdentifyAsync(KeylessAnonUser);

                AssertHelpers.UsersEqualExcludingAutoProperties(
                    User.Builder(KeylessAnonUser).Key(client.User.Key).Build(),
                    client.User);
                Assert.Equal(client.User, stub.ReceivedUser);
            }
        }

        [Fact]
        public void SharedClientIsTheOnlyClientAvailable()
        {
            TestUtil.WithClientLock(() =>
            {
                var config = BasicConfig().Build();
                using (var client = LdClient.Init(config, BasicUser, TimeSpan.Zero))
                {
                    Assert.Throws<Exception>(() => LdClient.Init(config, BasicUser, TimeSpan.Zero));
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
                var config = BasicConfig().Build();
                using (var client0 = LdClient.Init(config, BasicUser, TimeSpan.Zero)) { }
                Assert.Null(LdClient.Instance);
                // Dispose() is called automatically at end of "using" block
                using (var client1 = LdClient.Init(config, BasicUser, TimeSpan.Zero)) { }
            });
        }

        [Fact]
        public void ConnectionChangeShouldStopDataSource()
        {
            var mockUpdateProc = new MockPollingProcessor(null);
            var mockConnectivityStateManager = new MockConnectivityStateManager(true);
            var config = BasicConfig()
                .DataSource(mockUpdateProc.AsFactory())
                .ConnectivityStateManager(mockConnectivityStateManager)
                .Build();
            using (var client = TestUtil.CreateClient(config, BasicUser))
            {
                mockConnectivityStateManager.Connect(false);
                Assert.False(mockUpdateProc.IsRunning);
            }
        }

        [Fact]
        public void FlagsAreLoadedFromPersistentStorageByDefault()
        {
            var storage = new MockPersistentDataStore();
            var data = new DataSetBuilder().Add("flag", 1, LdValue.Of(100), 0).Build();
            var config = BasicConfig()
                .Persistence(Components.Persistence().Storage(storage.AsSingletonFactory()))
                .Offline(true)
                .Build();
            storage.SetupUserData(config.MobileKey, BasicUser.Key, data);

            using (var client = TestUtil.CreateClient(config, BasicUser))
            {
                Assert.Equal(100, client.IntVariation("flag", 99));
            }
        }

        [Fact]
        public void FlagsAreSavedToPersistentStorageByDefault()
        {
            var storage = new MockPersistentDataStore();
            var initialFlags = new DataSetBuilder().Add("flag", 1, LdValue.Of(100), 0).Build();
            var config = BasicConfig()
                .DataSource(MockPollingProcessor.Factory(initialFlags))
                .Persistence(Components.Persistence().Storage(storage.AsSingletonFactory()))
                .Build();

            using (var client = TestUtil.CreateClient(config, BasicUser))
            {
                var storedData = storage.InspectUserData(config.MobileKey, BasicUser.Key);
                Assert.NotNull(storedData);
                AssertHelpers.DataSetsEqual(initialFlags, storedData.Value);
            }
        }

        [Fact]
        public void EventProcessorIsOnlineByDefault()
        {
            var eventProcessor = new MockEventProcessor();
            var config = BasicConfig()
                .Events(eventProcessor.AsSingletonFactory())
                .Build();
            using (var client = TestUtil.CreateClient(config, BasicUser))
            {
                Assert.False(eventProcessor.Offline);
            }
        }

        [Fact]
        public void EventProcessorIsOfflineWhenClientIsConfiguredOffline()
        {
            var connectivityStateManager = new MockConnectivityStateManager(true);
            var eventProcessor = new MockEventProcessor();
            var config = BasicConfig()
                .ConnectivityStateManager(connectivityStateManager)
                .Events(eventProcessor.AsSingletonFactory())
                .Offline(true)
                .Build();
            using (var client = TestUtil.CreateClient(config, BasicUser))
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
            var config = BasicConfig()
                .ConnectivityStateManager(connectivityStateManager)
                .Events(eventProcessor.AsSingletonFactory())
                .Build();
            using (var client = TestUtil.CreateClient(config, BasicUser))
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
