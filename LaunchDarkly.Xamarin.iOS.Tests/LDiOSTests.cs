using System;
using NUnit.Framework;

namespace LaunchDarkly.Xamarin.iOS.Tests
{
    [TestFixture]
    public class LDiOSTests
    {
        private ILdMobileClient client;

        [SetUp]
        public void Setup() {
            var user = LaunchDarkly.Client.User.WithKey("test-user");
            var timeSpan = TimeSpan.FromSeconds(10);
            client = LdClient.Init("mob-368413a0-28e1-495d-ab32-7aa389ac33b6", user, timeSpan);
        }

        [TearDown]
        public void Tear() { LdClient.Instance = null;}

        [Test]
        public void BooleanFeatureFlag()
        {
            Console.WriteLine("Test Boolean Variation");
            Assert.True(client.BoolVariation("boolean-feature-flag"));
        }

        [Test]
        public void IntFeatureFlag()
        {
            Console.WriteLine("Test Integer Variation");
            Assert.True(client.IntVariation("int-feature-flag") == 2);
        }
    }
}
