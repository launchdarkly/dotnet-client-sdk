using System;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.Sdk.Client.Interfaces;
using Xunit;
using Xunit.Abstractions;

using static LaunchDarkly.Sdk.Client.Interfaces.DataStoreTypes;
using static LaunchDarkly.Sdk.Client.Interfaces.EventProcessorTypes;

namespace LaunchDarkly.Sdk.Client
{
    public class LdClientEventTests : BaseTest
    {
        private static readonly User user = User.WithKey("userkey");
        private readonly TestData _testData = TestData.DataSource();
        private MockEventProcessor eventProcessor = new MockEventProcessor();
        private IEventProcessorFactory _factory;

        public LdClientEventTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            _factory = eventProcessor.AsSingletonFactory();
        }

        private LdClient MakeClient(User u) =>
            LdClient.Init(BasicConfig().DataSource(_testData).Events(_factory).Build(),
                u, TimeSpan.FromSeconds(1));

        [Fact]
        public void IdentifySendsIdentifyEvent()
        {
            using (LdClient client = MakeClient(user))
            {
                User user1 = User.WithKey("userkey1");
                client.Identify(user1, TimeSpan.FromSeconds(1));
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user), // there's always an initial identify event
                    e => CheckIdentifyEvent(e, user1));
            }
        }

        [Fact]
        public void TrackSendsCustomEvent()
        {
            using (LdClient client = MakeClient(user))
            {
                client.Track("eventkey");
                Assert.Collection(eventProcessor.Events, 
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        CustomEvent ce = Assert.IsType<CustomEvent>(e);
                        Assert.Equal("eventkey", ce.EventKey);
                        Assert.Equal(user.Key, ce.User.Key);
                        Assert.Equal(LdValue.Null, ce.Data);
                        Assert.Null(ce.MetricValue);
                        Assert.NotEqual(0, ce.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void TrackWithDataSendsCustomEvent()
        {
            using (LdClient client = MakeClient(user))
            {
                LdValue data = LdValue.Of("hi");
                client.Track("eventkey", data);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        CustomEvent ce = Assert.IsType<CustomEvent>(e);
                        Assert.Equal("eventkey", ce.EventKey);
                        Assert.Equal(user.Key, ce.User.Key);
                        Assert.Equal(data, ce.Data);
                        Assert.Null(ce.MetricValue);
                        Assert.NotEqual(0, ce.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void TrackWithMetricValueSendsCustomEvent()
        {
            using (LdClient client = MakeClient(user))
            {
                LdValue data = LdValue.Of("hi");
                double metricValue = 1.5;
                client.Track("eventkey", data, metricValue);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        CustomEvent ce = Assert.IsType<CustomEvent>(e);
                        Assert.Equal("eventkey", ce.EventKey);
                        Assert.Equal(user.Key, ce.User.Key);
                        Assert.Equal(data, ce.Data);
                        Assert.Equal(metricValue, ce.MetricValue);
                        Assert.NotEqual(0, ce.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void AliasSendsAliasEvent()
        {
            User oldUser = User.Builder("anon-key").Anonymous(true).Build();
            User newUser = User.WithKey("real-key");

            using (LdClient client = MakeClient(user))
            {
                client.Alias(user, oldUser);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        AliasEvent ae = Assert.IsType<AliasEvent>(e);
                        Assert.Equal(user, ae.User);
                        Assert.Equal(oldUser, ae.PreviousUser);
                        Assert.NotEqual(0, ae.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void IdentifySendsAliasEventFromAnonUserToNonAnonUserIfNotOptedOut()
        {
            User oldUser = User.Builder("anon-key").Anonymous(true).Build();
            User newUser = User.WithKey("real-key");

            using (LdClient client = MakeClient(oldUser))
            {
                User actualOldUser = client.User; // so we can get any automatic properties that the client added
                client.Identify(newUser, TimeSpan.FromSeconds(1));

                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, actualOldUser),
                    e => CheckIdentifyEvent(e, newUser),
                    e => {
                        AliasEvent ae = Assert.IsType<AliasEvent>(e);
                        Assert.Equal(newUser, ae.User);
                        Assert.Equal(actualOldUser, ae.PreviousUser);
                        Assert.NotEqual(0, ae.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void IdentifyDoesNotSendAliasEventIfOptedOUt()
        {
            User oldUser = User.Builder("anon-key").Anonymous(true).Build();
            User newUser = User.WithKey("real-key");

            var config = BasicConfig()
                .Events(_factory)
                .AutoAliasingOptOut(true)
                .Build();
            
            using (LdClient client = TestUtil.CreateClient(config, oldUser))
            {
                User actualOldUser = client.User; // so we can get any automatic properties that the client added
                client.Identify(newUser, TimeSpan.FromSeconds(1));

                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, actualOldUser),
                    e => CheckIdentifyEvent(e, newUser));
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void IdentifyDoesNotSendAliasEventIfNewUserIsAnonymousOrOldUserIsNot(
            bool oldAnon, bool newAnon)
        {
            User oldUser = User.Builder("old-key").Anonymous(oldAnon).Build();
            User newUser = User.Builder("new-key").Anonymous(newAnon).Build();

            using (LdClient client = MakeClient(oldUser))
            {
                client.Identify(newUser, TimeSpan.FromSeconds(1));

                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, oldUser),
                    e => CheckIdentifyEvent(e, newUser));
            }
        }

        [Fact]
        public void VariationSendsFeatureEventForValidFlag()
        {
            var flag = new FeatureFlagBuilder().Value(LdValue.Of("a")).Variation(1).Version(1000)
                .TrackEvents(true).DebugEventsUntilDate(UnixMillisecondTime.OfMillis(2000)).Build();
            _testData.Update(_testData.Flag("flag").PreconfiguredFlag(flag));
            using (LdClient client = MakeClient(user))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("a", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        EvaluationEvent fe = Assert.IsType<EvaluationEvent>(e);
                        Assert.Equal("flag", fe.FlagKey);
                        Assert.Equal("a", fe.Value.AsString);
                        Assert.Equal(1, fe.Variation);
                        Assert.Equal(1000, fe.FlagVersion);
                        Assert.Equal("b", fe.Default.AsString);
                        Assert.True(fe.TrackEvents);
                        Assert.Equal(UnixMillisecondTime.OfMillis(2000), fe.DebugEventsUntilDate);
                        Assert.Null(fe.Reason);
                        Assert.NotEqual(0, fe.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void FeatureEventUsesFlagVersionIfProvided()
        {
            var flag = new FeatureFlagBuilder().Value(LdValue.Of("a")).Variation(1).Version(1000)
                .FlagVersion(1500).Build();
            _testData.Update(_testData.Flag("flag").PreconfiguredFlag(flag));
            using (LdClient client = MakeClient(user))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("a", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        EvaluationEvent fe = Assert.IsType<EvaluationEvent>(e);
                        Assert.Equal("flag", fe.FlagKey);
                        Assert.Equal("a", fe.Value.AsString);
                        Assert.Equal(1, fe.Variation);
                        Assert.Equal(1500, fe.FlagVersion);
                        Assert.Equal("b", fe.Default.AsString);
                        Assert.NotEqual(0, fe.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void VariationSendsFeatureEventForDefaultValue()
        {
            var flag = new FeatureFlagBuilder().Version(1000).Build();
            _testData.Update(_testData.Flag("flag").PreconfiguredFlag(flag));
            using (LdClient client = MakeClient(user))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("b", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        EvaluationEvent fe = Assert.IsType<EvaluationEvent>(e);
                        Assert.Equal("flag", fe.FlagKey);
                        Assert.Equal("b", fe.Value.AsString);
                        Assert.Null(fe.Variation);
                        Assert.Equal(1000, fe.FlagVersion);
                        Assert.Equal("b", fe.Default.AsString);
                        Assert.Null(fe.Reason);
                        Assert.NotEqual(0, fe.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void VariationSendsFeatureEventForUnknownFlag()
        {
            using (LdClient client = MakeClient(user))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("b", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        EvaluationEvent fe = Assert.IsType<EvaluationEvent>(e);
                        Assert.Equal("flag", fe.FlagKey);
                        Assert.Equal("b", fe.Value.AsString);
                        Assert.Null(fe.Variation);
                        Assert.Null(fe.FlagVersion);
                        Assert.Equal("b", fe.Default.AsString);
                        Assert.Null(fe.Reason);
                        Assert.NotEqual(0, fe.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void VariationSendsFeatureEventForUnknownFlagWhenClientIsNotInitialized()
        {
            var config = BasicConfig()
                .DataSource(new MockDataSourceThatNeverInitializes().AsSingletonFactory())
                .Events(_factory);

            using (LdClient client = TestUtil.CreateClient(config.Build(), user))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("b", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e =>
                    {
                        EvaluationEvent fe = Assert.IsType<EvaluationEvent>(e);
                        Assert.Equal("flag", fe.FlagKey);
                        Assert.Equal("b", fe.Value.AsString);
                        Assert.Null(fe.Variation);
                        Assert.Null(fe.FlagVersion);
                        Assert.Equal("b", fe.Default.AsString);
                        Assert.Null(fe.Reason);
                        Assert.NotEqual(0, fe.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void VariationSendsFeatureEventWithTrackingAndReasonIfTrackReasonIsTrue()
        {
            var flag = new FeatureFlagBuilder().Value(LdValue.Of("a")).Variation(1).Version(1000)
                .TrackReason(true).Reason(EvaluationReason.OffReason).Build();
            _testData.Update(_testData.Flag("flag").PreconfiguredFlag(flag));
            using (LdClient client = MakeClient(user))
            {
                string result = client.StringVariation("flag", "b");
                Assert.Equal("a", result);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        EvaluationEvent fe = Assert.IsType<EvaluationEvent>(e);
                        Assert.Equal("flag", fe.FlagKey);
                        Assert.Equal("a", fe.Value.AsString);
                        Assert.Equal(1, fe.Variation);
                        Assert.Equal(1000, fe.FlagVersion);
                        Assert.Equal("b", fe.Default.AsString);
                        Assert.True(fe.TrackEvents);
                        Assert.Null(fe.DebugEventsUntilDate);
                        Assert.Equal(EvaluationReason.OffReason, fe.Reason);
                        Assert.NotEqual(0, fe.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void VariationDetailSendsFeatureEventWithReasonForValidFlag()
        {
            var flag = new FeatureFlagBuilder().Value(LdValue.Of("a")).Variation(1).Version(1000)
                .TrackEvents(true).DebugEventsUntilDate(UnixMillisecondTime.OfMillis(2000))
                .Reason(EvaluationReason.OffReason).Build();
            _testData.Update(_testData.Flag("flag").PreconfiguredFlag(flag));
            using (LdClient client = MakeClient(user))
            {
                EvaluationDetail<string> result = client.StringVariationDetail("flag", "b");
                Assert.Equal("a", result.Value);
                Assert.Equal(EvaluationReason.OffReason, result.Reason);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        EvaluationEvent fe = Assert.IsType<EvaluationEvent>(e);
                        Assert.Equal("flag", fe.FlagKey);
                        Assert.Equal("a", fe.Value.AsString);
                        Assert.Equal(1, fe.Variation);
                        Assert.Equal(1000, fe.FlagVersion);
                        Assert.Equal("b", fe.Default.AsString);
                        Assert.True(fe.TrackEvents);
                        Assert.Equal(UnixMillisecondTime.OfMillis(2000), fe.DebugEventsUntilDate);
                        Assert.Equal(EvaluationReason.OffReason, fe.Reason);
                        Assert.NotEqual(0, fe.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void VariationDetailSendsFeatureEventWithReasonForUnknownFlag()
        {
            using (LdClient client = MakeClient(user))
            {
                EvaluationDetail<string> result = client.StringVariationDetail("flag", "b");
                var expectedReason = EvaluationReason.ErrorReason(EvaluationErrorKind.FlagNotFound);
                Assert.Equal("b", result.Value);
                Assert.Equal(expectedReason, result.Reason);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        EvaluationEvent fe = Assert.IsType<EvaluationEvent>(e);
                        Assert.Equal("flag", fe.FlagKey);
                        Assert.Equal("b", fe.Value.AsString);
                        Assert.Null(fe.Variation);
                        Assert.Null(fe.FlagVersion);
                        Assert.Equal("b", fe.Default.AsString);
                        Assert.False(fe.TrackEvents);
                        Assert.Null(fe.DebugEventsUntilDate);
                        Assert.Equal(expectedReason, fe.Reason);
                        Assert.NotEqual(0, fe.Timestamp.Value);
                    });
            }
        }

        [Fact]
        public void VariationSendsFeatureEventWithReasonForUnknownFlagWhenClientIsNotInitialized()
        {
            var config = BasicConfig()
                .DataSource(new MockDataSourceThatNeverInitializes().AsSingletonFactory())
                .Events(_factory);

            using (LdClient client = TestUtil.CreateClient(config.Build(), user))
            {
                EvaluationDetail<string> result = client.StringVariationDetail("flag", "b");
                var expectedReason = EvaluationReason.ErrorReason(EvaluationErrorKind.ClientNotReady);
                Assert.Equal("b", result.Value);
                Assert.Equal(expectedReason, result.Reason);
                Assert.Collection(eventProcessor.Events,
                    e => CheckIdentifyEvent(e, user),
                    e => {
                        EvaluationEvent fe = Assert.IsType<EvaluationEvent>(e);
                        Assert.Equal("flag", fe.FlagKey);
                        Assert.Equal("b", fe.Value.AsString);
                        Assert.Null(fe.Variation);
                        Assert.Null(fe.FlagVersion);
                        Assert.Equal("b", fe.Default.AsString);
                        Assert.False(fe.TrackEvents);
                        Assert.Null(fe.DebugEventsUntilDate);
                        Assert.Equal(expectedReason, fe.Reason);
                        Assert.NotEqual(0, fe.Timestamp.Value);
                    });
            }
        }

        private void CheckIdentifyEvent(object e, User u)
        {
            IdentifyEvent ie = Assert.IsType<IdentifyEvent>(e);
            Assert.Equal(u.Key, ie.User.Key);
            Assert.NotEqual(0, ie.Timestamp.Value);
        }
    }
}
