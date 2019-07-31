using LaunchDarkly.Client;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class LdClientEvaluationTests : BaseTest
    {
        static readonly string appKey = "some app key";
        static readonly string nonexistentFlagKey = "some flag key";
        static readonly User user = User.WithKey("userkey");

        private static LdClient ClientWithFlagsJson(string flagsJson)
        {
            var config = TestUtil.ConfigWithFlagsJson(user, appKey, flagsJson).Build();
            return TestUtil.CreateClient(config, user);
        }

        [Fact]
        public void BoolVariationReturnsValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(true));
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                Assert.True(client.BoolVariation("flag-key", false));
            }
        }

        [Fact]
        public void BoolVariationReturnsDefaultForUnknownFlag()
        {
            using (var client = ClientWithFlagsJson("{}"))
            {
                Assert.False(client.BoolVariation(nonexistentFlagKey));
            }
        }

        [Fact]
        public void BoolVariationDetailReturnsValue()
        {
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(true), 1, reason);
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                var expected = new EvaluationDetail<bool>(true, 1, reason);
                Assert.Equal(expected, client.BoolVariationDetail("flag-key", false));
            }
        }

        [Fact]
        public void IntVariationReturnsValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(3));
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                Assert.Equal(3, client.IntVariation("flag-key", 0));
            }
        }

        [Fact]
        public void IntVariationCoercesFloatValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(3.0f));
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                Assert.Equal(3, client.IntVariation("flag-key", 0));
            }
        }

        [Fact]
        public void IntVariationReturnsDefaultForUnknownFlag()
        {
            using (var client = ClientWithFlagsJson("{}"))
            {
                Assert.Equal(1, client.IntVariation(nonexistentFlagKey, 1));
            }
        }

        [Fact]
        public void IntVariationDetailReturnsValue()
        {
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(3), 1, reason);
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                var expected = new EvaluationDetail<int>(3, 1, reason);
                Assert.Equal(expected, client.IntVariationDetail("flag-key", 0));
            }
        }

        [Fact]
        public void FloatVariationReturnsValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(2.5f));
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                Assert.Equal(2.5f, client.FloatVariation("flag-key", 0));
            }
        }

        [Fact]
        public void FloatVariationCoercesIntValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(2));
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                Assert.Equal(2.0f, client.FloatVariation("flag-key", 0));
            }
        }

        [Fact]
        public void FloatVariationReturnsDefaultForUnknownFlag()
        {
            using (var client = ClientWithFlagsJson("{}"))
            {
                Assert.Equal(0.5f, client.FloatVariation(nonexistentFlagKey, 0.5f));
            }
        }

        [Fact]
        public void FloatVariationDetailReturnsValue()
        {
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(2.5f), 1, reason);
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                var expected = new EvaluationDetail<float>(2.5f, 1, reason);
                Assert.Equal(expected, client.FloatVariationDetail("flag-key", 0.5f));
            }
        }

        [Fact]
        public void StringVariationReturnsValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue("string value"));
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                Assert.Equal("string value", client.StringVariation("flag-key", ""));
            }
        }

        [Fact]
        public void StringVariationReturnsDefaultForUnknownFlag()
        {
            using (var client = ClientWithFlagsJson("{}"))
            {
                Assert.Equal("d", client.StringVariation(nonexistentFlagKey, "d"));
            }
        }

        [Fact]
        public void StringVariationDetailReturnsValue()
        {
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue("string value"), 1, reason);
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                var expected = new EvaluationDetail<string>("string value", 1, reason);
                Assert.Equal(expected, client.StringVariationDetail("flag-key", ""));
            }
        }

        [Fact]
        public void JsonVariationReturnsValue()
        {
            var jsonValue = new JObject { { "thing", new JValue("stuff") } };
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", jsonValue);
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                Assert.Equal(jsonValue, client.JsonVariation("flag-key", ImmutableJsonValue.FromJToken(3)).AsJToken());
            }
        }

        [Fact]
        public void JsonVariationReturnsDefaultForUnknownFlag()
        {
            using (var client = ClientWithFlagsJson("{}"))
            {
                var defaultVal = ImmutableJsonValue.FromJToken(3);
                Assert.Equal(defaultVal, client.JsonVariation(nonexistentFlagKey, defaultVal));
            }
        }

        [Fact]
        public void JsonVariationDetailReturnsValue()
        {
            var jsonValue = new JObject { { "thing", new JValue("stuff") } };
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", jsonValue, 1, reason);
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                var expected = new EvaluationDetail<JToken>(jsonValue, 1, reason);
                var result = client.JsonVariationDetail("flag-key", ImmutableJsonValue.FromJToken(3));
                // Note, JToken.Equals() doesn't work, so we need to test each property separately
                Assert.True(JToken.DeepEquals(expected.Value, result.Value.AsJToken()));
                Assert.Equal(expected.VariationIndex, result.VariationIndex);
                Assert.Equal(expected.Reason, result.Reason);
            }
        }

        [Fact]
        public void AllFlagsReturnsAllFlagValues()
        {
            var flagsJson = @"{""flag1"":{""value"":""a""},""flag2"":{""value"":""b""}}";
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                var result = client.AllFlags();
                Assert.Equal(2, result.Count);
                Assert.Equal("a", result["flag1"].AsString);
                Assert.Equal("b", result["flag2"].AsString);
            }
        }
        
        [Fact]
        public void DefaultValueReturnedIfValueTypeIsDifferent()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue("string value"));
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                Assert.Equal(3, client.IntVariation("flag-key", 3));
            }
        }

        [Fact]
        public void DefaultValueAndReasonIsReturnedIfValueTypeIsDifferent()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue("string value"));
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                var expected = new EvaluationDetail<int>(3, null, new EvaluationReason.Error(EvaluationErrorKind.WRONG_TYPE));
                Assert.Equal(expected, client.IntVariationDetail("flag-key", 3));
            }
        }

        [Fact]
        public void DefaultValueReturnedIfFlagValueIsNull()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", null);
            using (var client = ClientWithFlagsJson(flagsJson))
            {
                Assert.Equal(3, client.IntVariation("flag-key", 3));
            }
        }
    }
}
