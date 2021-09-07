﻿using System;
using Xunit;

namespace LaunchDarkly.Sdk.Client.Integrations
{
    public class StreamingDataSourceBuilderTest
    {
        private readonly BuilderInternalTestUtil<StreamingDataSourceBuilder> _tester =
            BuilderTestUtil.For(Components.StreamingDataSource);

        [Fact]
        public void BackgroundPollInterval()
        {
            var prop = _tester.Property(b => b._backgroundPollInterval, (b, v) => b.BackgroundPollInterval(v));
            prop.AssertDefault(Configuration.DefaultBackgroundPollInterval);
            prop.AssertCanSet(TimeSpan.FromMinutes(90));
            prop.AssertSetIsChangedTo(TimeSpan.FromMilliseconds(222), Configuration.MinimumBackgroundPollInterval);
        }

        [Fact]
        public void BaseUri()
        {
            var prop = _tester.Property(b => b._baseUri, (b, v) => b.BaseUri(v));
            prop.AssertDefault(null);
            prop.AssertCanSet(new Uri("http://x"));
        }

        [Fact]
        public void InitialReconnectDelay()
        {
            var prop = _tester.Property(b => b._initialReconnectDelay, (b, v) => b.InitialReconnectDelay(v));
            prop.AssertDefault(StreamingDataSourceBuilder.DefaultInitialReconnectDelay);
            prop.AssertCanSet(TimeSpan.FromMilliseconds(222));
        }

        [Fact]
        public void PollingBaseUri()
        {
            var prop = _tester.Property(b => b._pollingBaseUri, (b, v) => b.PollingBaseUri(v));
            prop.AssertDefault(null);
            prop.AssertCanSet(new Uri("http://x"));
        }
    }
}
