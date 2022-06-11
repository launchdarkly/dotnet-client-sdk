using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client
{
    public class LdClientTests : BaseTest
    {
        private static readonly Context KeylessAnonUser = TestUtil.BuildAutoContext()
                .Set("email", "example")
                .Set("other", 3)
                .Build();

        public LdClientTests(ITestOutputHelper testOutput) : base(testOutput) { }

        [Fact]
        public void CannotCreateClientWithNullConfig()
        {
            Assert.Throws<ArgumentNullException>(() => LdClient.Init((Configuration)null, BasicUser, TimeSpan.Zero));
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
                var actualUser = client.Context; // may have been transformed e.g. to add device/OS properties
                Assert.Equal(BasicUser.Key, actualUser.Key);
                Assert.Equal(actualUser, stub.ReceivedContext);
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
                Assert.Equal("fake-device-id", client.Context.Key);
                AssertHelpers.ContextsEqual(
                    Context.BuilderFromContext(KeylessAnonUser).Key(client.Context.Key).Build(),
                    client.Context);
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
                generatedKey = client.Context.Key;
                Assert.NotNull(generatedKey);
                AssertHelpers.ContextsEqual(
                    Context.BuilderFromContext(KeylessAnonUser).Key(client.Context.Key).Build(),
                    client.Context);
            }

            using (var client = await TestUtil.CreateClientAsync(config, KeylessAnonUser))
            {
                Assert.Equal(generatedKey, client.Context.Key);
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
                generatedKey = client.Context.Key;
                Assert.NotNull(generatedKey);
                AssertHelpers.ContextsEqual(
                    Context.BuilderFromContext(KeylessAnonUser).Key(client.Context.Key).Build(),
                    client.Context);
            }

            using (var client = await TestUtil.CreateClientAsync(config, KeylessAnonUser))
            {
                Assert.Equal(generatedKey, client.Context.Key);
            }

            // Now use a configuration where persistence is disabled - a different key is generated
            var configWithoutPersistence = BasicConfig().Persistence(Components.NoPersistence).Build();
            using (var client = await TestUtil.CreateClientAsync(configWithoutPersistence, KeylessAnonUser))
            {
                Assert.NotNull(client.Context.Key);
                Assert.NotEqual(generatedKey, client.Context.Key);
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
                AssertHelpers.ContextsEqual(
                    Context.BuilderFromContext(KeylessAnonUser).Key(stub.ReceivedContext.Key).Build(),
                    stub.ReceivedContext);
            }
        }

        [Fact]
        public void IdentifyUpdatesTheUser()
        {
            using (var client = TestUtil.CreateClient(BasicConfig().Build(), BasicUser))
            {
                var updatedUser = Context.New("some new key");
                var success = client.Identify(updatedUser, TimeSpan.FromSeconds(1));
                Assert.True(success);
                Assert.Equal(client.Context.Key, updatedUser.Key); // don't compare entire user, because SDK may have added device/os attributes
            }
        }

        [Fact]
        public Task IdentifyAsyncCompletesOnlyWhenNewFlagsAreAvailable()
            => IdentifyCompletesOnlyWhenNewFlagsAreAvailable((client, context) => client.IdentifyAsync(context));

        [Fact]
        public Task IdentifySyncCompletesOnlyWhenNewFlagsAreAvailable()
            => IdentifyCompletesOnlyWhenNewFlagsAreAvailable((client, context) => Task.Run(() => client.Identify(context, TimeSpan.FromSeconds(4))));

        private async Task IdentifyCompletesOnlyWhenNewFlagsAreAvailable(Func<LdClient, Context, Task> identifyTask)
        {
            var userA = Context.New("a");
            var userB = Context.New("b");

            var flagKey = "flag";
            var userAFlags = new DataSetBuilder()
                .Add(flagKey, 1, LdValue.Of("a-value"), 0).Build();
            var userBFlags = new DataSetBuilder()
                .Add(flagKey, 2, LdValue.Of("b-value"), 1).Build();

            var startedIdentifyUserB = new SemaphoreSlim(0, 1);
            var canFinishIdentifyUserB = new SemaphoreSlim(0, 1);
            var finishedIdentifyUserB = new SemaphoreSlim(0, 1);

            var dataSourceFactory = new MockDataSourceFactoryFromLambda((ctx, updates, context, bg) =>
                new MockDataSourceFromLambda(context, async () =>
                {
                    switch (context.Key)
                    {
                        case "a":
                            updates.Init(context, userAFlags);
                            break;

                        case "b":
                            startedIdentifyUserB.Release();
                            await canFinishIdentifyUserB.WaitAsync();
                            updates.Init(context, userBFlags);
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
        public async void IdentifyPassesUserToDataSource()
        {
            MockPollingProcessor stub = new MockPollingProcessor(DataSetBuilder.Empty);
            Context newUser = Context.New("new-user");

            var config = BasicConfig()
                .DataSource(stub.AsFactory())
                .Build();

            using (var client = await LdClient.InitAsync(config, BasicUser))
            {
                AssertHelpers.ContextsEqual(BasicUser, client.Context);
                Assert.Equal(client.Context, stub.ReceivedContext);

                await client.IdentifyAsync(newUser);

                AssertHelpers.ContextsEqual(newUser, client.Context);
                Assert.Equal(client.Context, stub.ReceivedContext);
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

                Assert.Equal("fake-device-id", client.Context.Key);
                AssertHelpers.ContextsEqual(
                    Context.BuilderFromContext(KeylessAnonUser).Key(client.Context.Key).Build(),
                    client.Context);
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

                generatedKey = client.Context.Key;
                Assert.NotNull(generatedKey);
                AssertHelpers.ContextsEqual(
                    Context.BuilderFromContext(KeylessAnonUser).Key(client.Context.Key).Build(),
                    client.Context);
            }

            using (var client = await TestUtil.CreateClientAsync(config, BasicUser))
            {
                client.Identify(KeylessAnonUser, TimeSpan.FromSeconds(1));

                Assert.Equal(generatedKey, client.Context.Key);
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

                generatedKey = client.Context.Key;
                Assert.NotNull(generatedKey);
                AssertHelpers.ContextsEqual(
                    Context.BuilderFromContext(KeylessAnonUser).Key(client.Context.Key).Build(),
                    client.Context);
             }

            using (var client = await TestUtil.CreateClientAsync(config, BasicUser))
            {
                await client.IdentifyAsync(KeylessAnonUser);

                Assert.Equal(generatedKey, client.Context.Key);
            }

            // Now use a configuration where persistence is disabled - a different key is generated
            var configWithoutPersistence = BasicConfig().Persistence(Components.NoPersistence).Build();
            using (var client = await TestUtil.CreateClientAsync(configWithoutPersistence, BasicUser))
            {
                await client.IdentifyAsync(KeylessAnonUser);

                Assert.NotNull(client.Context.Key);
                Assert.NotEqual(generatedKey, client.Context.Key);
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

                AssertHelpers.ContextsEqual(
                    Context.BuilderFromContext(KeylessAnonUser).Key(client.Context.Key).Build(),
                    client.Context);
                Assert.Equal(client.Context, stub.ReceivedContext);
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
