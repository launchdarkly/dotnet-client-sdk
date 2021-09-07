using System;
using System.Net.Http;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client
{
    public class ConfigurationTest : BaseTest
    {
        private readonly BuilderBehavior.BuildTester<ConfigurationBuilder, Configuration> _tester =
            BuilderBehavior.For(() => Configuration.Builder(mobileKey), b => b.Build())
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
        public void EnableBackgroundUpdating()
        {
            var prop = _tester.Property(c => c.EnableBackgroundUpdating, (b, v) => b.EnableBackgroundUpdating(v));
            prop.AssertDefault(true);
            prop.AssertCanSet(false);
        }

        [Fact]
        public void EvaluationReasons()
        {
            var prop = _tester.Property(c => c.EvaluationReasons, (b, v) => b.EvaluationReasons(v));
            prop.AssertDefault(false);
            prop.AssertCanSet(true);
        }

        [Fact]
        public void Events()
        {
            var prop = _tester.Property(c => c.EventProcessorFactory, (b, v) => b.Events(v));
            prop.AssertDefault(null);
            prop.AssertCanSet(new ComponentsImpl.NullEventProcessorFactory());
        }

        [Fact]
        public void HttpMessageHandler()
        {
            var prop = _tester.Property(c => c.HttpMessageHandler, (b, v) => b.HttpMessageHandler(v));
            // Can't test the default here because the default is platform-dependent.
            prop.AssertCanSet(new HttpClientHandler());
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
        public void PersistFlagValues()
        {
            var prop = _tester.Property(c => c.PersistFlagValues, (b, v) => b.PersistFlagValues(v));
            prop.AssertDefault(true);
            prop.AssertCanSet(false);
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