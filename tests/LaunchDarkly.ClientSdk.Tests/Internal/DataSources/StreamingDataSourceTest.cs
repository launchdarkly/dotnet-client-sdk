using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LaunchDarkly.EventSource;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.Sdk.Internal.Http;
using LaunchDarkly.Sdk.Json;
using Xunit;
using Xunit.Abstractions;

using static LaunchDarkly.Sdk.Client.TestUtil;
using static LaunchDarkly.TestHelpers.JsonAssertions;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    public class StreamingDataSourceTest : BaseTest
    {
        private const string initialFlagsJson = "{" +
            "\"int-flag\":{\"value\":15,\"version\":100}," +
            "\"float-flag\":{\"value\":13.5,\"version\":100}," +
            "\"string-flag\":{\"value\":\"markw@magenic.com\",\"version\":100}" +
            "}";

        private readonly User user = User.WithKey("me");
        private const string encodedUser = "eyJrZXkiOiJtZSJ9";

        private EventSourceMock mockEventSource;
        private TestEventSourceFactory eventSourceFactory;
        private MockDataSourceUpdateSink _updateSink = new MockDataSourceUpdateSink();
        private IFeatureFlagRequestor mockRequestor;
        private Uri baseUri;
        private TimeSpan initialReconnectDelay = StreamingDataSourceBuilder.DefaultInitialReconnectDelay;
        private bool withReasons = false;

        public StreamingDataSourceTest(ITestOutputHelper testOutput) : base(testOutput)
        {
            mockEventSource = new EventSourceMock();
            eventSourceFactory = new TestEventSourceFactory(mockEventSource);
            mockRequestor = new MockFeatureFlagRequestor(initialFlagsJson);
            baseUri = new Uri("http://example");
        }

        private StreamingDataSource MakeStartedStreamingDataSource()
        {
            var dataSource = new StreamingDataSource(
                _updateSink,
                user,
                baseUri,
                withReasons,
                initialReconnectDelay,
                mockRequestor,
                TestUtil.SimpleContext.Http,
                testLogger,
                eventSourceFactory.Create()
                );
            dataSource.Start();
            return dataSource;
        }

        [Theory]
        [InlineData("", false, "/meval/", "")]
        [InlineData("", true, "/meval/", "?withReasons=true")]
        [InlineData("/basepath", false, "/basepath/meval/", "")]
        [InlineData("/basepath", true, "/basepath/meval/", "?withReasons=true")]
        [InlineData("/basepath/", false, "/basepath/meval/", "")]
        [InlineData("/basepath/", true, "/basepath/meval/", "?withReasons=true")]
        public void RequestHasCorrectUriAndMethodInGetMode(
            string baseUriExtraPath,
            bool withReasons,
            string expectedPathWithoutUser,
            string expectedQuery
            )
        {
            var fakeRootUri = "http://fake-stream-host";
            var fakeBaseUri = fakeRootUri + baseUriExtraPath;
            this.baseUri = new Uri(fakeBaseUri);
            this.withReasons = withReasons;
            MakeStartedStreamingDataSource();
            Assert.Equal(HttpMethod.Get, eventSourceFactory.ReceivedMethod);
            Assert.Equal(new Uri(fakeRootUri + expectedPathWithoutUser + encodedUser + expectedQuery),
                eventSourceFactory.ReceivedUri);
        }

        // REPORT mode is known to fail in Android (ch47341)
#if !__ANDROID__
        [Theory]
        [InlineData("", false, "/meval", "")]
        [InlineData("", true, "/meval", "?withReasons=true")]
        [InlineData("/basepath", false, "/basepath/meval", "")]
        [InlineData("/basepath", true, "/basepath/meval", "?withReasons=true")]
        [InlineData("/basepath/", false, "/basepath/meval", "")]
        [InlineData("/basepath/", true, "/basepath/meval", "?withReasons=true")]
        public void RequestHasCorrectUriAndMethodAndBodyInReportMode(
            string baseUriExtraPath,
            bool withReasons,
            string expectedPath,
            string expectedQuery
            )
        {
            var fakeRootUri = "http://fake-stream-host";
            var fakeBaseUri = fakeRootUri + baseUriExtraPath;
            this.baseUri = new Uri(fakeBaseUri);
            var httpConfig = Components.HttpConfiguration().UseReport(true).CreateHttpConfiguration(SimpleContext.Basic);
            using (var dataSource = new StreamingDataSource(
                _updateSink,
                user,
                baseUri,
                withReasons,
                initialReconnectDelay,
                mockRequestor,
                httpConfig,
                testLogger,
                eventSourceFactory.Create()
                ))
            {
                dataSource.Start();

                Assert.Equal(new HttpMethod("REPORT"), eventSourceFactory.ReceivedMethod);
                Assert.Equal(new Uri(fakeRootUri + expectedPath + expectedQuery),
                    eventSourceFactory.ReceivedUri);
                Assert.NotNull(eventSourceFactory.ReceivedBody);
                AssertJsonEqual(LdJsonSerialization.SerializeObject(user),
                        NormalizeJsonUser(LdValue.Parse(eventSourceFactory.ReceivedBody)));
            }
        }
#endif

        [Fact]
        public void PutStoresFeatureFlags()
        {
            MakeStartedStreamingDataSource();
            // should be empty before PUT message arrives
            _updateSink.Actions.ExpectNoValue();

            PUTMessageSentToProcessor();

            var gotData = _updateSink.ExpectInit(user);
            var gotItem = gotData.Items.First(item => item.Key == "int-flag");
            int intFlagValue = gotItem.Value.Item.Value.AsInt;
            Assert.Equal(15, intFlagValue);
        }

        [Fact]
        public void PatchUpdatesFeatureFlag()
        {
            // before PATCH, fill in flags
            MakeStartedStreamingDataSource();
            PUTMessageSentToProcessor();
            _updateSink.ExpectInit(user);

            //PATCH to update 1 flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent("patch", UpdatedFlag(), null));
            mockEventSource.RaiseMessageRcvd(eventArgs);

            //verify flag has changed
            var gotItem = _updateSink.ExpectUpsert(user, "int-flag");
            Assert.Equal(99, gotItem.Item.Value.AsInt);
        }

        [Fact]
        public void DeleteRemovesFeatureFlag()
        {
            // before DELETE, fill in flags, test it's there
            MakeStartedStreamingDataSource();
            PUTMessageSentToProcessor();
            _updateSink.ExpectInit(user);

            // DELETE int-flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent("delete", DeleteFlag(), null));
            mockEventSource.RaiseMessageRcvd(eventArgs);

            // verify flag was deleted
            var gotItem = _updateSink.ExpectUpsert(user, "int-flag");
            Assert.Null(gotItem.Item);
        }

        [Fact]
        public void PingCausesPoll()
        {
            MakeStartedStreamingDataSource();
            mockEventSource.RaiseMessageRcvd(new MessageReceivedEventArgs(new MessageEvent("ping", null, null)));

            var gotData = _updateSink.ExpectInit(user);
            var gotItem = gotData.Items.First(item => item.Key == "int-flag");
            int intFlagValue = gotItem.Value.Item.Value.AsInt;
            Assert.Equal(15, intFlagValue);
        }

        string UpdatedFlag()
        {
            var updatedFlagAsJson = "{\"key\":\"int-flag\",\"version\":999,\"flagVersion\":192,\"value\":99,\"variation\":0,\"trackEvents\":false}";
            return updatedFlagAsJson;
        }

        string DeleteFlag()
        {
            var flagToDelete = "{\"key\":\"int-flag\",\"version\":1214}";
            return flagToDelete;
        }

        void PUTMessageSentToProcessor()
        {
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent("put", initialFlagsJson, null));
            mockEventSource.RaiseMessageRcvd(eventArgs);
        }
    }

    class TestEventSourceFactory
    {
        public HttpProperties ReceivedHttpProperties { get; private set; }
        public HttpMethod ReceivedMethod { get; private set; }
        public Uri ReceivedUri { get; private set; }
        public string ReceivedBody { get; private set; }
        IEventSource _eventSource;

        public TestEventSourceFactory(IEventSource eventSource)
        {
            _eventSource = eventSource;
        }

        public StreamingDataSource.EventSourceCreator Create()
        {
            return (httpProperties, method, uri, jsonBody) =>
            {
                ReceivedHttpProperties = httpProperties;
                ReceivedMethod = method;
                ReceivedUri = uri;
                ReceivedBody = jsonBody;
                return _eventSource;
            };
        }
    }

    class EventSourceMock : IEventSource
    {
        public ReadyState ReadyState => throw new NotImplementedException();

#pragma warning disable 0067 // unused properties
        public event EventHandler<StateChangedEventArgs> Opened;
        public event EventHandler<StateChangedEventArgs> Closed;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<CommentReceivedEventArgs> CommentReceived;
        public event EventHandler<ExceptionEventArgs> Error;
#pragma warning restore 0067

        public void Close()
        {
            
        }

        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public void Restart(bool withDelay) { }

        public void RaiseMessageRcvd(MessageReceivedEventArgs eventArgs)
        {
            MessageReceived(null, eventArgs);
        }
    }
}
