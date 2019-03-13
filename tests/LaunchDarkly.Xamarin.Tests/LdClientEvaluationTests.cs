using System;
using System.Collections.Generic;
using System.Text;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class LdClientEvaluationTests
    {
        static readonly string appKey = "some app key";
        static readonly string nonexistentFlagKey = "some flag key";
        static readonly User user = User.WithKey("userkey");
        
        private static LdClient ClientWithFlagsJson(string flagsJson)
        {
            var config = TestUtil.ConfigWithFlagsJson(user, appKey, flagsJson);
            return TestUtil.CreateClient(config, user);
        }

        [Fact]
        public void BoolVariationReturnsValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(true));
            var client = ClientWithFlagsJson(flagsJson);

            Assert.True(client.BoolVariation("flag-key", false));
        }

        [Fact]
        public void BoolVariationReturnsDefaultForUnknownFlag()
        {
            var client = ClientWithFlagsJson("{}");
            Assert.False(client.BoolVariation(nonexistentFlagKey));
        }

        [Fact]
        public void BoolVariationDetailReturnsValue()
        {
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(true), 1, reason);
            var client = ClientWithFlagsJson(flagsJson);

            var expected = new EvaluationDetail<bool>(true, 1, reason);
            Assert.Equal(expected, client.BoolVariationDetail("flag-key", false));
        }

        [Fact]
        public void IntVariationReturnsValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(3));
            var client = ClientWithFlagsJson(flagsJson);

            Assert.Equal(3, client.IntVariation("flag-key", 0));
        }

        [Fact]
        public void IntVariationCoercesFloatValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(3.0f));
            var client = ClientWithFlagsJson(flagsJson);

            Assert.Equal(3, client.IntVariation("flag-key", 0));
        }

        [Fact]
        public void IntVariationReturnsDefaultForUnknownFlag()
        {
            var client = ClientWithFlagsJson("{}");
            Assert.Equal(1, client.IntVariation(nonexistentFlagKey, 1));
        }

        [Fact]
        public void IntVariationDetailReturnsValue()
        {
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(3), 1, reason);
            var client = ClientWithFlagsJson(flagsJson);

            var expected = new EvaluationDetail<int>(3, 1, reason);
            Assert.Equal(expected, client.IntVariationDetail("flag-key", 0));
        }

        [Fact]
        public void FloatVariationReturnsValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(2.5f));
            var client = ClientWithFlagsJson(flagsJson);

            Assert.Equal(2.5f, client.FloatVariation("flag-key", 0));
        }

        [Fact]
        public void FloatVariationCoercesIntValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(2));
            var client = ClientWithFlagsJson(flagsJson);

            Assert.Equal(2.0f, client.FloatVariation("flag-key", 0));
        }

        [Fact]
        public void FloatVariationReturnsDefaultForUnknownFlag()
        {
            var client = ClientWithFlagsJson("{}");
            Assert.Equal(0.5f, client.FloatVariation(nonexistentFlagKey, 0.5f));
        }

        [Fact]
        public void FloatVariationDetailReturnsValue()
        {
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue(2.5f), 1, reason);
            var client = ClientWithFlagsJson(flagsJson);

            var expected = new EvaluationDetail<float>(2.5f, 1, reason);
            Assert.Equal(expected, client.FloatVariationDetail("flag-key", 0.5f));
        }

        [Fact]
        public void StringVariationReturnsValue()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue("string value"));
            var client = ClientWithFlagsJson(flagsJson);

            Assert.Equal("string value", client.StringVariation("flag-key", ""));
        }

        [Fact]
        public void StringVariationReturnsDefaultForUnknownFlag()
        {
            var client = ClientWithFlagsJson("{}");
            Assert.Equal("d", client.StringVariation(nonexistentFlagKey, "d"));
        }

        [Fact]
        public void StringVariationDetailReturnsValue()
        {
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue("string value"), 1, reason);
            var client = ClientWithFlagsJson(flagsJson);

            var expected = new EvaluationDetail<string>("string value", 1, reason);
            Assert.Equal(expected, client.StringVariationDetail("flag-key", ""));
        }

        [Fact]
        public void JsonVariationReturnsValue()
        {
            var jsonValue = new JObject { { "thing", new JValue("stuff") } };
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", jsonValue);
            var client = ClientWithFlagsJson(flagsJson);

            var defaultValue = new JValue(3);
            Assert.Equal(jsonValue, client.JsonVariation("flag-key", defaultValue));
        }

        [Fact]
        public void JsonVariationReturnsDefaultForUnknownFlag()
        {
            var client = ClientWithFlagsJson("{}");
            Assert.Null(client.JsonVariation(nonexistentFlagKey, null));
        }

        [Fact]
        public void JsonVariationDetailReturnsValue()
        {
            var jsonValue = new JObject { { "thing", new JValue("stuff") } };
            var reason = EvaluationReason.Off.Instance;
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", jsonValue, 1, reason);
            var client = ClientWithFlagsJson(flagsJson);

            var expected = new EvaluationDetail<JToken>(jsonValue, 1, reason);
            var result = client.JsonVariationDetail("flag-key", new JValue(3));
            // Note, JToken.Equals() doesn't work, so we need to test each property separately
            Assert.True(JToken.DeepEquals(expected.Value, result.Value));
            Assert.Equal(expected.VariationIndex, result.VariationIndex);
            Assert.Equal(expected.Reason, result.Reason);
        }

        [Fact]
        public void AllFlagsReturnsAllFlagValues()
        {
            var flagsJson = @"{""flag1"":{""value"":""a""},""flag2"":{""value"":""b""}}";
            var client = ClientWithFlagsJson(flagsJson);

            var result = client.AllFlags();
            Assert.Equal(2, result.Count);
            Assert.Equal(new JValue("a"), result["flag1"]);
            Assert.Equal(new JValue("b"), result["flag2"]);
        }
        
        [Fact]
        public void DefaultValueReturnedIfValueTypeIsDifferent()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue("string value"));
            var config = TestUtil.ConfigWithFlagsJson(user, appKey, flagsJson);
            var client = TestUtil.CreateClient(config, user);

            Assert.Equal(3, client.IntVariation("flag-key", 3));
        }

        [Fact]
        public void DefaultValueAndReasonIsReturnedIfValueTypeIsDifferent()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", new JValue("string value"));
            var config = TestUtil.ConfigWithFlagsJson(user, appKey, flagsJson);
            var client = TestUtil.CreateClient(config, user);

            var expected = new EvaluationDetail<int>(3, null, new EvaluationReason.Error(EvaluationErrorKind.WRONG_TYPE));
            Assert.Equal(expected, client.IntVariationDetail("flag-key", 3));
        }

        [Fact]
        public void DefaultValueReturnedIfFlagValueIsNull()
        {
            string flagsJson = TestUtil.JsonFlagsWithSingleFlag("flag-key", null);
            var config = TestUtil.ConfigWithFlagsJson(user, appKey, flagsJson);
            var client = TestUtil.CreateClient(config, user);

            Assert.Equal(3, client.IntVariation("flag-key", 3));
        }
    }
}
