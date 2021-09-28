using System;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.Sdk.Client.Interfaces;
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
            var testData = TestData.DataSource();
            var config = TestUtil.TestConfig("mobile-key")
                .DataSource(testData)
                .Logging(testLogging)
                .Build();

            var flagKey = "flagkey";
            testData.Update(testData.Flag(flagKey).Variation(true));

            using (var client = TestUtil.CreateClient(config, user))
            {
                var eventSink1 = new EventSink<FlagValueChangeEvent>();
                var eventSink2 = new EventSink<FlagValueChangeEvent>();
                EventHandler<FlagValueChangeEvent> listener1 = eventSink1.Add;
                EventHandler<FlagValueChangeEvent> listener2 = eventSink2.Add;
                client.FlagTracker.FlagValueChanged += listener1;
                client.FlagTracker.FlagValueChanged += listener2;

                eventSink1.ExpectNoValue();
                eventSink2.ExpectNoValue();

                testData.Update(testData.Flag(flagKey).Variation(false));

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
