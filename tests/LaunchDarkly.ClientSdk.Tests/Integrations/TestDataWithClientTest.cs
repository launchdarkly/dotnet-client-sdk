using System;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client.Integrations
{
    public class TestDataWithClientTest : BaseTest
    {
        private readonly TestData _td = TestData.DataSource();
        private readonly Configuration _config;
        private readonly User _user = User.WithKey("userkey");

        public TestDataWithClientTest(ITestOutputHelper testOutput) : base(testOutput)
        {
            _config = Configuration.Builder("mobile-key")
                .DataSource(_td)
                .Events(Components.NoEvents)
                .Build();
        }

        [Fact]
        public void InitializesWithEmptyData()
        {
            using (var client = LdClient.Init(_config, _user, TimeSpan.FromSeconds(1)))
            {
                Assert.True(client.Initialized);
            }
        }

        [Fact]
        public void InitializesWithFlag()
        {
            _td.Update(_td.Flag("flag").Variation(true));

            using (var client = LdClient.Init(_config, _user, TimeSpan.FromSeconds(1)))
            {
                Assert.True(client.BoolVariation("flag", false));
            }
        }

        [Fact]
        public void UpdatesFlag()
        {
            using (var client = LdClient.Init(_config, _user, TimeSpan.FromSeconds(1)))
            {
                Assert.False(client.BoolVariation("flag", false));

                _td.Update(_td.Flag("flag").Variation(true));

                Assert.True(client.BoolVariation("flag", false));
            }
        }

        [Fact]
        public void CanSetValuePerUser()
        {
            _td.Update(_td.Flag("flag")
                .Variations(LdValue.Of("red"), LdValue.Of("green"), LdValue.Of("blue"))
                .Variation(LdValue.Of("red"))
                .VariationForUser("user1", LdValue.Of("green"))
                .VariationForUser("user2", LdValue.Of("blue"))
                .VariationFunc(user =>
                    user.GetAttribute(UserAttribute.ForName("favoriteColor"))
                ));
            var user1 = User.WithKey("user1");
            var user2 = User.WithKey("user2");
            var user3 = User.Builder("user3").Custom("favoriteColor", "green").Build();

            using (var client = LdClient.Init(_config, user1, TimeSpan.FromSeconds(1)))
            {
                Assert.Equal("green", client.StringVariation("flag", ""));

                client.Identify(user2, TimeSpan.FromSeconds(1));

                Assert.Equal("blue", client.StringVariation("flag", ""));

                client.Identify(user3, TimeSpan.FromSeconds(1));

                Assert.Equal("green", client.StringVariation("flag", ""));
            }
        }
    }
}
