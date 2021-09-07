using System;
using System.Collections.Immutable;
using System.Net.Http;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client
{
    public class ConfigurationTest : BaseTest
    {
        private readonly BuilderTestUtil<ConfigurationBuilder, Configuration> _tester =
            BuilderTestUtil.For(() => Configuration.Builder(mobileKey), b => b.Build())
                .WithCopyConstructor(c => Configuration.Builder(c));

        const string mobileKey = "any-key";

        public ConfigurationTest(ITestOutputHelper testOutput) : base(testOutput) { }

        [Fact]
        public void DefaultSetsKey()
        {
            var config = Configuration.Default(mobileKey);
            Assert.Equal(mobileKey, config.MobileKey);
        }

        [Fact]
        public void BuilderSetsKey()
        {
            var config = Configuration.Builder(mobileKey).Build();
            Assert.Equal(mobileKey, config.MobileKey);
        }

        [Fact]
        public void AutoAliasingOptOut()
        {
            var prop = _tester.Property(c => c.AutoAliasingOptOut, (b, v) => b.AutoAliasingOptOut(v));
            prop.AssertDefault(false);
            prop.AssertCanSet(true);
        }

        [Fact]
        public void DataSource()
        {
            var prop = _tester.Property(c => c.DataSourceFactory, (b, v) => b.DataSource(v));
            prop.AssertDefault(null);
            prop.AssertCanSet(new ComponentsImpl.NullDataSourceFactory());
        }

        [Fact]
        public void AllAttributesPrivate()
        {
            var prop = _tester.Property(b => b.AllAttributesPrivate, (b, v) => b.AllAttributesPrivate(v));
            prop.AssertDefault(false);
            prop.AssertCanSet(true);
        }

        [Fact]
        public void EventCapacity()
        {
            var prop = _tester.Property(b => b.EventCapacity, (b, v) => b.EventCapacity(v));
            prop.AssertDefault(Configuration.DefaultEventCapacity);
            prop.AssertCanSet(1);
            prop.AssertSetIsChangedTo(0, Configuration.DefaultEventCapacity);
            prop.AssertSetIsChangedTo(-1, Configuration.DefaultEventCapacity);
        }

        [Fact]
        public void EventsUri()
        {
            var prop = _tester.Property(b => b.EventsUri, (b, v) => b.EventsUri(v));
            prop.AssertDefault(Configuration.DefaultEventsUri);
            prop.AssertCanSet(new Uri("http://x"));
            prop.AssertSetIsChangedTo(null, Configuration.DefaultEventsUri);
        }

        [Fact]
        public void FlushInterval()
        {
            var prop = _tester.Property(b => b.EventFlushInterval, (b, v) => b.EventFlushInterval(v));
            prop.AssertDefault(Configuration.DefaultEventFlushInterval);
            prop.AssertCanSet(TimeSpan.FromMinutes(7));
            prop.AssertSetIsChangedTo(TimeSpan.Zero, Configuration.DefaultEventFlushInterval);
            prop.AssertSetIsChangedTo(TimeSpan.FromMilliseconds(-1), Configuration.DefaultEventFlushInterval);
        }

        [Fact]
        public void HttpMessageHandler()
        {
            var prop = _tester.Property(c => c.HttpMessageHandler, (b, v) => b.HttpMessageHandler(v));
            // Can't test the default here because the default is platform-dependent.
            prop.AssertCanSet(new HttpClientHandler());
        }

        [Fact]
        public void InlineUsersInEvents()
        {
            var prop = _tester.Property(b => b.InlineUsersInEvents, (b, v) => b.InlineUsersInEvents(v));
            prop.AssertDefault(false);
            prop.AssertCanSet(true);
        }

        [Fact]
        public void Logging()
        {
            var prop = _tester.Property(c => c.LoggingConfigurationFactory, (b, v) => b.Logging(v));
            prop.AssertDefault(null);
            prop.AssertCanSet(Components.Logging(Logs.ToWriter(Console.Out)));
        }

        [Fact]
        public void LoggingAdapterShortcut()
        {
            var adapter = Logs.ToWriter(Console.Out);
            var config = Configuration.Builder("key").Logging(adapter).Build();
            var logConfig = config.LoggingConfigurationFactory.CreateLoggingConfiguration();
            Assert.Same(adapter, logConfig.LogAdapter);
        }

        [Fact]
        public void MobileKey()
        {
            var prop = _tester.Property(c => c.MobileKey, (b, v) => b.MobileKey(v));
            prop.AssertCanSet("other-key");
        }

        [Fact]
        public void Offline()
        {
            var prop = _tester.Property(c => c.Offline, (b, v) => b.Offline(v));
            prop.AssertDefault(false);
            prop.AssertCanSet(true);
        }

        [Fact]
        public void PrivateAttributes()
        {
            var b = _tester.New();
            Assert.Null(b.Build().PrivateAttributeNames);
            b.PrivateAttribute(UserAttribute.Name);
            b.PrivateAttribute(UserAttribute.Email);
            b.PrivateAttribute(UserAttribute.ForName("other"));
            Assert.Equal(ImmutableHashSet.Create<UserAttribute>(
                UserAttribute.Name, UserAttribute.Email, UserAttribute.ForName("other")), b.Build().PrivateAttributeNames);
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
    }
}