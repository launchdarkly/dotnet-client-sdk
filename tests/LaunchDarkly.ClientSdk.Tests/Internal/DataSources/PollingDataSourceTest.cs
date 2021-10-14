﻿using System;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Internal;
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

        private InMemoryDataStore _store = new InMemoryDataStore();
        User user;

        public PollingDataSourceTest(ITestOutputHelper testOutput) : base(testOutput) { }

        IDataSource MakeDataSource()
        {
            var mockFeatureFlagRequestor = new MockFeatureFlagRequestor(flagsJson);
            user = User.WithKey("user1Key");
            return new PollingDataSource(
                new DataSourceUpdateSinkImpl(_store, false, BasicTaskExecutor, testLogger),
                user,
                mockFeatureFlagRequestor,
                TimeSpan.FromSeconds(30),
                TimeSpan.Zero,
                BasicTaskExecutor,
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
            var flags = _store.GetAll(user);
            Assert.Equal(3, flags.Value.Items.Count);
        }
    }
}