using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Client.Subsystems;
using LaunchDarkly.TestHelpers;
using Xunit;

namespace LaunchDarkly.Sdk.Client.Integrations
{
    public class HttpConfigurationBuilderTest
    {
        private static readonly LdClientContext basicContext = LdClientContext.MakeMinimalContext("mobile-key");

        private readonly BuilderBehavior.BuildTester<HttpConfigurationBuilder, HttpConfiguration> _tester =
            BuilderBehavior.For(() => Components.HttpConfiguration(),
                b => b.CreateHttpConfiguration(basicContext));

        [Fact]
        public void ConnectTimeout()
        {
            var prop = _tester.Property(c => c.ConnectTimeout, (b, v) => b.ConnectTimeout(v));
            prop.AssertDefault(HttpConfigurationBuilder.DefaultConnectTimeout);
            prop.AssertCanSet(TimeSpan.FromSeconds(7));
        }

        [Fact]
        public void CustomHeaders()
        {
            var config = Components.HttpConfiguration()
                .CustomHeader("header1", "value1")
                .CustomHeader("header2", "value2")
                .CreateHttpConfiguration(basicContext);
            Assert.Equal("value1", HeadersAsMap(config.DefaultHeaders)["header1"]);
            Assert.Equal("value2", HeadersAsMap(config.DefaultHeaders)["header2"]);
        }

        [Fact]
        public void MessageHandler()
        {
            var prop = _tester.Property(c => c.MessageHandler, (b, v) => b.MessageHandler(v));
            // Can't test the default here because the default is platform-dependent.
            prop.AssertCanSet(new HttpClientHandler());
        }

        [Fact]
        public void MobileKeyHeader()
        {
            var config = Components.HttpConfiguration().CreateHttpConfiguration(basicContext);
            Assert.Equal(basicContext.MobileKey, HeadersAsMap(config.DefaultHeaders)["authorization"]);
        }

        [Fact]
        public void ResponseStartTimeout()
        {
            var value = TimeSpan.FromMilliseconds(789);
            var prop = _tester.Property(c => c.ResponseStartTimeout, (b, v) => b.ResponseStartTimeout(v));
            prop.AssertDefault(HttpConfigurationBuilder.DefaultResponseStartTimeout);
            prop.AssertCanSet(value);

            var config = Components.HttpConfiguration().ResponseStartTimeout(value)
                .CreateHttpConfiguration(basicContext);
            using (var client = config.NewHttpClient())
            {
                Assert.Equal(value, client.Timeout);
            }
        }

        [Fact]
        public void UseReport()
        {
            var prop = _tester.Property(c => c.UseReport, (b, v) => b.UseReport(v));
            prop.AssertDefault(false);
            prop.AssertCanSet(true);
        }

        [Fact]
        public void UserAgentHeader()
        {
            var config = Components.HttpConfiguration().CreateHttpConfiguration(basicContext);
            Assert.Equal("XamarinClient/" + AssemblyVersions.GetAssemblyVersionStringForType(typeof(LdClient)),
                HeadersAsMap(config.DefaultHeaders)["user-agent"]); // not configurable
        }

        [Fact]
        public void WrapperDefaultNone()
        {
            var config = Components.HttpConfiguration().CreateHttpConfiguration(basicContext);
            Assert.False(HeadersAsMap(config.DefaultHeaders).ContainsKey("x-launchdarkly-wrapper"));
        }

        [Fact]
        public void WrapperNameOnly()
        {
            var config = Components.HttpConfiguration().Wrapper("w", null)
                .CreateHttpConfiguration(basicContext);
            Assert.Equal("w", HeadersAsMap(config.DefaultHeaders)["x-launchdarkly-wrapper"]);
        }

        [Fact]
        public void WrapperNameAndVersion()
        {
            var config = Components.HttpConfiguration().Wrapper("w", "1.0")
                .CreateHttpConfiguration(basicContext);
            Assert.Equal("w/1.0", HeadersAsMap(config.DefaultHeaders)["x-launchdarkly-wrapper"]);
        }

        private static Dictionary<string, string> HeadersAsMap(IEnumerable<KeyValuePair<string, string>> headers)
        {
            return headers.ToDictionary(kv => kv.Key.ToLower(), kv => kv.Value);
        }
    }
}
