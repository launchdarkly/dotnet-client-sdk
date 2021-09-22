using System;
using LaunchDarkly.Sdk.Client.Interfaces;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client
{
    public class LdClientListenersTest : BaseTest
    {
        public LdClientListenersTest(ITestOutputHelper testOutput) : base(testOutput) { }

        [Fact]
        public void ClientSendsFlagValueChangeEvents()
        {
            var user = User.WithKey("user-key");
            IDataSourceUpdateSink updateSink = null;
            var mockDataSourceFactory = new MockDataSourceFactoryFromLambda((ctx, up, u, bg) =>
            {
                updateSink = up;
                return new ComponentsImpl.NullDataSource();
            });
            var config = TestUtil.TestConfig("mobile-key")
                .DataSource(mockDataSourceFactory)
                .Logging(testLogging)
                .Build();

            var flagKey = "flagkey";
            var initialData = new DataSetBuilder()
                .Add(flagKey, 1, LdValue.Of(true), 0)
                .Build();

            using (var client = TestUtil.CreateClient(config, user))
            {
                updateSink.Init(user, initialData);

                var eventSink1 = new EventSink<FlagValueChangeEvent>();
                var eventSink2 = new EventSink<FlagValueChangeEvent>();
                EventHandler<FlagValueChangeEvent> listener1 = eventSink1.Add;
                EventHandler<FlagValueChangeEvent> listener2 = eventSink2.Add;
                client.FlagTracker.FlagValueChanged += listener1;
                client.FlagTracker.FlagValueChanged += listener2;

                eventSink1.ExpectNoValue();
                eventSink2.ExpectNoValue();

                var updatedFlag = new FeatureFlagBuilder()
                    .Version(2)
                    .Value(LdValue.Of(false))
                    .Variation(1)
                    .Build();
                updateSink.Upsert(user, flagKey, updatedFlag.ToItemDescriptor());

                var event1 = eventSink1.ExpectValue();
                var event2 = eventSink2.ExpectValue();
                Assert.Equal(flagKey, event1.Key);
                Assert.Equal(LdValue.Of(true), event1.OldValue);
                Assert.Equal(LdValue.Of(false), event1.NewValue);
                Assert.Equal(event1, event2);

                eventSink1.ExpectNoValue();
                eventSink2.ExpectNoValue();
            }
        }
    }
}
