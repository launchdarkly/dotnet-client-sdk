using System;
using System.Net.Http;
using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class ConfigurationTest
    {
        [Fact]
        public void CanOverrideConfiguration()
        {
            var config = Configuration.Default("AnyOtherSdkKey")
                .WithBaseUri("https://app.AnyOtherEndpoint.com")
                .WithEventQueueCapacity(99)
                .WithPollingInterval(TimeSpan.FromMinutes(1));

            Assert.Equal(new Uri("https://app.AnyOtherEndpoint.com"), config.BaseUri);
            Assert.Equal("AnyOtherSdkKey", config.MobileKey);
            Assert.Equal(99, config.EventQueueCapacity);
            Assert.Equal(TimeSpan.FromMinutes(1), config.PollingInterval);
        }

        [Fact]
        public void CanOverrideStreamConfiguration()
        {
            var config = Configuration.Default("AnyOtherSdkKey")
                .WithStreamUri("https://stream.AnyOtherEndpoint.com")
                .WithIsStreamingEnabled(false)
                .WithReadTimeout(TimeSpan.FromDays(1))
                .WithReconnectTime(TimeSpan.FromDays(1));

            Assert.Equal(new Uri("https://stream.AnyOtherEndpoint.com"), config.StreamUri);
            Assert.False(config.IsStreamingEnabled);
            Assert.Equal(TimeSpan.FromDays(1), config.ReadTimeout);
            Assert.Equal(TimeSpan.FromDays(1), config.ReconnectTime);
        }
        
        [Fact]
        public void MobileKeyCannotBeNull()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Configuration.Default(null));
        }

        [Fact]
        public void MobileKeyCannotBeEmpty()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Configuration.Default(""));
        }

        [Fact]
        public void CannotOverrideTooSmallPollingInterval()
        {
            var config = Configuration.Default("AnyOtherSdkKey").WithPollingInterval(TimeSpan.FromSeconds(29));

            Assert.Equal(TimeSpan.FromSeconds(30), config.PollingInterval);
        }

        [Fact]
        public void CanSetHttpClientHandler()
        {
            var handler = new HttpClientHandler();
            var config = Configuration.Default("AnyOtherSdkKey")
                .WithHttpClientHandler(handler);

            Assert.Equal(handler, config.HttpClientHandler);
        }
    }
}