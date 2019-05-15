using System;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Xamarin.Android.Tests
{
    [TestFixture]
    public class LDAndroidTests
    {
        private ILdMobileClient client;

        [SetUp]
        public void Setup()
        {
            var user = LaunchDarkly.Client.User.WithKey("test-user");
            var timeSpan = TimeSpan.FromSeconds(10);
            client = LdClient.Init("MOBILE_KEY", user, timeSpan);
        }

        [TearDown]
        public void Tear() { LdClient.Instance = null; }

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

        [Test]
        public void StringFeatureFlag()
        {
            Console.WriteLine("Test String Variation");
            Assert.True(client.StringVariation("string-feature-flag", "false").Equals("bravo"));
        }

        [Test]
        public void JsonFeatureFlag()
        {
            string json = @"{
            ""test2"": ""testing2""
            }";
            Console.WriteLine("Test JSON Variation");
            JToken jsonToken = JToken.FromObject(JObject.Parse(json));
            Assert.True(JToken.DeepEquals(jsonToken, client.JsonVariation("json-feature-flag", "false")));
        }

        [Test]
        public void FloatFeatureFlag()
        {
            Console.WriteLine("Test Float Variation");
            Assert.True(client.FloatVariation("float-feature-flag") == 1.5);
        }
    }
}
