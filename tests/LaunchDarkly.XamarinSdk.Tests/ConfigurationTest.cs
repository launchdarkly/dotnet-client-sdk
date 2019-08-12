using System;
using System.Net.Http;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class ConfigurationTest : BaseTest
    {
        [Fact]
        public void CanOverrideConfiguration()
        {
            var config = Configuration.Builder("AnyOtherSdkKey")
                .BaseUri(new Uri("https://app.AnyOtherEndpoint.com"))
                .EventCapacity(99)
                .PollingInterval(TimeSpan.FromMinutes(45))
                .Build();

            Assert.Equal(new Uri("https://app.AnyOtherEndpoint.com"), config.BaseUri);
            Assert.Equal("AnyOtherSdkKey", config.MobileKey);
            Assert.Equal(99, config.EventCapacity);
            Assert.Equal(TimeSpan.FromMinutes(45), config.PollingInterval);
        }

        [Fact]
        public void CanOverrideStreamConfiguration()
        {
            var config = Configuration.Builder("AnyOtherSdkKey")
                .StreamUri(new Uri("https://stream.AnyOtherEndpoint.com"))
                .IsStreamingEnabled(false)
                .ReadTimeout(TimeSpan.FromDays(1))
                .ReconnectTime(TimeSpan.FromDays(1))
                .Build();

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
        public void CannotSetTooSmallPollingInterval()
        {
            var config = Configuration.Builder("AnyOtherSdkKey").PollingInterval(TimeSpan.FromSeconds(299)).Build();

            Assert.Equal(TimeSpan.FromSeconds(300), config.PollingInterval);
        }

        [Fact]
        public void CannotSetTooSmallBackgroundPollingInterval()
        {
            var config = Configuration.Builder("SdkKey").BackgroundPollingInterval(TimeSpan.FromSeconds(899)).Build();

            Assert.Equal(TimeSpan.FromSeconds(900), config.BackgroundPollingInterval);
        }

        [Fact]
        public void CanSetHttpMessageHandler()
        {
            var handler = new HttpClientHandler();
            var config = Configuration.Builder("AnyOtherSdkKey")
                .HttpMessageHandler(handler)
                .Build();

            Assert.Equal(handler, config.HttpMessageHandler);
        }
    }
}