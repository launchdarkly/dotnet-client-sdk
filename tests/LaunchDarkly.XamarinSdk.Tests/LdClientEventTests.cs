using LaunchDarkly.Client;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class LdClientEventTests : BaseTest
    {
        private static readonly User user = User.WithKey("userkey");
        private MockEventProcessor eventProcessor = new MockEventProcessor();

        public LdClient MakeClient(User user, string flagsJson)
        {
            Configuration config = TestUtil.ConfigWithFlagsJson(user, "appkey", flagsJson);
            config.WithEventProcessor(eventProcessor);
            return TestUtil.CreateClient(config, user);
        }

        [Fact]
        public void IdentifySendsIdentifyEvent()
        {
            using (LdClient client = MakeClient(user, "{}"))
            {
                User user1 = User.WithKey("userkey1");
                client.Identify(user1);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user), // there's always an initial identify event
                    e => CheckIdentifyEvent(e, user1));
            }
        }

        [Fact]
        public void TrackSendsCustomEvent()
        {
            using (LdClient client = MakeClient(user, "{}"))
            {
                JToken data = new JValue("hi");
                client.Track("eventkey", ImmutableJsonValue.FromJToken(data));
                Assert.Collection(eventProcessor.Events, 
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        CustomEvent ce = Assert.IsType<CustomEvent>(e);
                        Assert.Equal("eventkey", ce.Key);
                        Assert.Equal(user.Key, ce.User.Key);
                        Assert.Equal(data, ce.JsonData);
                    });
            }
        }

        [Fact]
        public void VariationSendsFeatureEventForValidFlag()
        {
            string flagsJson = @"{""flag"":{
                ""value"":""a"",""variation"":1,""version"":1000,
                ""trackEvents"":true, ""debugEventsUntilDate"":2000 }}";
            using (LdClient client = MakeClient(user, flagsJson))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("a", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        FeatureRequestEvent fe = Assert.IsType<FeatureRequestEvent>(e);
                        Assert.Equal("flag", fe.Key);
                        Assert.Equal("a", fe.Value);
                        Assert.Equal(1, fe.Variation);
                        Assert.Equal(1000, fe.Version);
                        Assert.Equal("b", fe.Default);
                        Assert.True(fe.TrackEvents);
                        Assert.Equal(2000, fe.DebugEventsUntilDate);
                        Assert.Null(fe.Reason);
                    });
            }
        }

        [Fact]
        public void FeatureEventUsesFlagVersionIfProvided()
        {
            string flagsJson = @"{""flag"":{
                ""value"":""a"",""variation"":1,""version"":1000,
                ""flagVersion"":1500 }}";
            using (LdClient client = MakeClient(user, flagsJson))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("a", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        FeatureRequestEvent fe = Assert.IsType<FeatureRequestEvent>(e);
                        Assert.Equal("flag", fe.Key);
                        Assert.Equal("a", fe.Value);
                        Assert.Equal(1, fe.Variation);
                        Assert.Equal(1500, fe.Version);
                        Assert.Equal("b", fe.Default);
                    });
            }
        }

        [Fact]
        public void VariationSendsFeatureEventForDefaultValue()
        {
            string flagsJson = @"{""flag"":{
                ""value"":null,""variation"":null,""version"":1000 }}";
            using (LdClient client = MakeClient(user, flagsJson))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("b", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        FeatureRequestEvent fe = Assert.IsType<FeatureRequestEvent>(e);
                        Assert.Equal("flag", fe.Key);
                        Assert.Equal("b", fe.Value);
                        Assert.Null(fe.Variation);
                        Assert.Equal(1000, fe.Version);
                        Assert.Equal("b", fe.Default);
                        Assert.Null(fe.Reason);
                    });
            }
        }

        [Fact]
        public void VariationSendsFeatureEventForUnknownFlag()
        {
            using (LdClient client = MakeClient(user, "{}"))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("b", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        FeatureRequestEvent fe = Assert.IsType<FeatureRequestEvent>(e);
                        Assert.Equal("flag", fe.Key);
                        Assert.Equal("b", fe.Value);
                        Assert.Null(fe.Variation);
                        Assert.Null(fe.Version);
                        Assert.Equal("b", fe.Default);
                        Assert.Null(fe.Reason);
                    });
            }
        }

        [Fact]
        public void VariationSendsFeatureEventWithTrackingAndReasonIfTrackReasonIsTrue()
        {
            string flagsJson = @"{""flag"":{
                ""value"":""a"",""variation"":1,""version"":1000,
                ""trackReason"":true, ""reason"":{""kind"":""OFF""}
                }}";
            using (LdClient client = MakeClient(user, flagsJson))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("a", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        FeatureRequestEvent fe = Assert.IsType<FeatureRequestEvent>(e);
                        Assert.Equal("flag", fe.Key);
                        Assert.Equal("a", fe.Value);
                        Assert.Equal(1, fe.Variation);
                        Assert.Equal(1000, fe.Version);
                        Assert.Equal("b", fe.Default);
                        Assert.True(fe.TrackEvents);
                        Assert.Null(fe.DebugEventsUntilDate);
                        Assert.Equal(EvaluationReason.Off.Instance, fe.Reason);
                    });
            }
        }

        [Fact]
        public void VariationDetailSendsFeatureEventWithReasonForValidFlag()
        {
            string flagsJson = @"{""flag"":{
                ""value"":""a"",""variation"":1,""version"":1000,
                ""trackEvents"":true, ""debugEventsUntilDate"":2000,
                ""reason"":{""kind"":""OFF""}
                }}";
            using (LdClient client = MakeClient(user, flagsJson))
            {
                EvaluationDetail<string> result = client.StringVariationDetail("flag", "b");
                Assert.Equal("a", result.Value);
                Assert.Equal(EvaluationReason.Off.Instance, result.Reason);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        FeatureRequestEvent fe = Assert.IsType<FeatureRequestEvent>(e);
                        Assert.Equal("flag", fe.Key);
                        Assert.Equal("a", fe.Value);
                        Assert.Equal(1, fe.Variation);
                        Assert.Equal(1000, fe.Version);
                        Assert.Equal("b", fe.Default);
                        Assert.True(fe.TrackEvents);
                        Assert.Equal(2000, fe.DebugEventsUntilDate);
                        Assert.Equal(EvaluationReason.Off.Instance, fe.Reason);
                    });
            }
        }

        [Fact]
        public void VariationDetailSendsFeatureEventWithReasonForUnknownFlag()
        {
            using (LdClient client = MakeClient(user, "{}"))
            {
                EvaluationDetail<string> result = client.StringVariationDetail("flag", "b");
                var expectedReason = new EvaluationReason.Error(EvaluationErrorKind.FLAG_NOT_FOUND);
                Assert.Equal("b", result.Value);
                Assert.Equal(expectedReason, result.Reason);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        FeatureRequestEvent fe = Assert.IsType<FeatureRequestEvent>(e);
                        Assert.Equal("flag", fe.Key);
                        Assert.Equal("b", fe.Value);
                        Assert.Null(fe.Variation);
                        Assert.Null(fe.Version);
                        Assert.Equal("b", fe.Default);
                        Assert.False(fe.TrackEvents);
                        Assert.Null(fe.DebugEventsUntilDate);
                        Assert.Equal(expectedReason, fe.Reason);
                    });
            }
        }

        private void CheckIdentifyEvent(Event e, User u)
        {
            IdentifyEvent ie = Assert.IsType<IdentifyEvent>(e);
            Assert.Equal(u.Key, ie.User.Key);
        }
    }
}
