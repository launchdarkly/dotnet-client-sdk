using System;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Xamarin
{
    public class ConfigurationTest : BaseTest
    {
        public ConfigurationTest(ITestOutputHelper testOutput) : base(testOutput) { }

        [Fact]
        public void TestDefaultsFromDefaultFactoryMethod()
        {
            VerifyDefaults(Configuration.Default("my-key"));
        }

        [Fact]
        public void TestDefaultsFromBuilder()
        {
            VerifyDefaults(Configuration.Builder("my-key").Build());
        }

        private void VerifyDefaults(Configuration c)
        {
            Assert.False(c.AllAttributesPrivate);
            Assert.Equal(Configuration.DefaultBackgroundPollingInterval, c.BackgroundPollingInterval);
            Assert.Equal(Configuration.DefaultUri, c.BaseUri);
            Assert.Equal(Configuration.DefaultConnectionTimeout, c.ConnectionTimeout);
            Assert.True(c.EnableBackgroundUpdating);
            Assert.False(c.EvaluationReasons);
            Assert.Equal(Configuration.DefaultEventCapacity, c.EventCapacity);
            Assert.Equal(Configuration.DefaultEventFlushInterval, c.EventFlushInterval);
            Assert.Equal(Configuration.DefaultEventsUri, c.EventsUri);
            Assert.False(c.InlineUsersInEvents);
            Assert.True(c.IsStreamingEnabled);
            Assert.False(c.Offline);
            Assert.True(c.PersistFlagValues);
            Assert.Equal(Configuration.DefaultPollingInterval, c.PollingInterval);
            Assert.Null(c.PrivateAttributeNames);
            Assert.Equal(Configuration.DefaultReadTimeout, c.ReadTimeout);
            Assert.Equal(Configuration.DefaultReconnectTime, c.ReconnectTime);
            Assert.Equal(Configuration.DefaultStreamUri, c.StreamUri);
            Assert.False(c.UseReport);
            Assert.Equal(Configuration.DefaultUserKeysCapacity, c.UserKeysCapacity);
            Assert.Equal(Configuration.DefaultUserKeysFlushInterval, c.UserKeysFlushInterval);
        }

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

            Assert.Same(handler, config.HttpMessageHandler);
        }
    }
}