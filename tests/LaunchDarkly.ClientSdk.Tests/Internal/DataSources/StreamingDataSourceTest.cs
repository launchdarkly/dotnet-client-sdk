using System;
using System.Net.Http;
using System.Threading.Tasks;
using LaunchDarkly.EventSource;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.Sdk.Client.Internal.DataStores;
using LaunchDarkly.Sdk.Client.Internal.Interfaces;
using LaunchDarkly.Sdk.Internal.Http;
using Xunit;
using Xunit.Abstractions;

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
        private IFlagCacheManager mockFlagCacheMgr;
        private IFeatureFlagRequestor mockRequestor;
        private Uri baseUri;
        private TimeSpan initialReconnectDelay = StreamingDataSourceBuilder.DefaultInitialReconnectDelay;
        private bool withReasons = false;

        public StreamingDataSourceTest(ITestOutputHelper testOutput) : base(testOutput)
        {
            mockEventSource = new EventSourceMock();
            eventSourceFactory = new TestEventSourceFactory(mockEventSource);
            mockFlagCacheMgr = new MockFlagCacheManager(new UserFlagInMemoryCache());
            mockRequestor = new MockFeatureFlagRequestor(initialFlagsJson);
            baseUri = new Uri("http://example");
        }

        private StreamingDataSource MakeStartedStreamingDataSource()
        {
            var dataSource = new StreamingDataSource(
                new DataSourceUpdateSinkImpl(mockFlagCacheMgr),
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

        // Report mode is currently disabled - ch47341
        //[Fact]
        //public void StreamUriInReportModeHasNoUser()
        //{
        //    var config = configBuilder.UseReport(true).Build();
        //    MobileStreamingProcessorStarted();
        //    var props = eventSourceFactory.ReceivedProperties;
        //    Assert.Equal(new HttpMethod("REPORT"), props.Method);
        //    Assert.Equal(new Uri(config.StreamUri, Constants.STREAM_REQUEST_PATH), props.StreamUri);
        //}

        //[Fact]
        //public void StreamUriInReportModeHasReasonsParameterIfConfigured()
        //{
        //    var config = configBuilder.UseReport(true).EvaluationReasons(true).Build();
        //    MobileStreamingProcessorStarted();
        //    var props = eventSourceFactory.ReceivedProperties;
        //    Assert.Equal(new Uri(config.StreamUri, Constants.STREAM_REQUEST_PATH + "?withReasons=true"), props.StreamUri);
        //}

        //[Fact]
        //public async Task StreamRequestBodyInReportModeHasUser()
        //{
        //    configBuilder.UseReport(true);
        //    MobileStreamingProcessorStarted();
        //    var props = eventSourceFactory.ReceivedProperties;
        //    var body = Assert.IsType<StringContent>(props.RequestBody);
        //    var s = await body.ReadAsStringAsync();
        //    Assert.Equal(user.AsJson(), s);
        //}

        [Fact]
        public void PutStoresFeatureFlags()
        {
            MakeStartedStreamingDataSource();
            // should be empty before PUT message arrives
            var flagsInCache = mockFlagCacheMgr.FlagsForUser(user);
            Assert.Empty(flagsInCache);

            PUTMessageSentToProcessor();
            flagsInCache = mockFlagCacheMgr.FlagsForUser(user);
            Assert.NotEmpty(flagsInCache);
            int intFlagValue = mockFlagCacheMgr.FlagForUser("int-flag", user).value.AsInt;
            Assert.Equal(15, intFlagValue);
        }

        [Fact]
        public void PatchUpdatesFeatureFlag()
        {
            // before PATCH, fill in flags
            MakeStartedStreamingDataSource();
            PUTMessageSentToProcessor();
            var intFlagFromPUT = mockFlagCacheMgr.FlagForUser("int-flag", user).value.AsInt;
            Assert.Equal(15, intFlagFromPUT);

            //PATCH to update 1 flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent("patch", UpdatedFlag(), null));
            mockEventSource.RaiseMessageRcvd(eventArgs);

            //verify flag has changed
            int flagFromPatch = mockFlagCacheMgr.FlagForUser("int-flag", user).value.AsInt;
            Assert.Equal(99, flagFromPatch);
        }

        [Fact]
        public void PatchDoesnotUpdateFlagIfVersionIsLower()
        {
            // before PATCH, fill in flags
            MakeStartedStreamingDataSource();
            PUTMessageSentToProcessor();
            var intFlagFromPUT = mockFlagCacheMgr.FlagForUser("int-flag", user).value.AsInt;
            Assert.Equal(15, intFlagFromPUT);

            //PATCH to update 1 flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent("patch", UpdatedFlagWithLowerVersion(), null));
            mockEventSource.RaiseMessageRcvd(eventArgs);

            //verify flag has not changed
            int flagFromPatch = mockFlagCacheMgr.FlagForUser("int-flag", user).value.AsInt;
            Assert.Equal(15, flagFromPatch);
        }

        [Fact]
        public void DeleteRemovesFeatureFlag()
        {
            // before DELETE, fill in flags, test it's there
            MakeStartedStreamingDataSource();
            PUTMessageSentToProcessor();
            var intFlagFromPUT = mockFlagCacheMgr.FlagForUser("int-flag", user).value.AsInt;
            Assert.Equal(15, intFlagFromPUT);

            // DELETE int-flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent("delete", DeleteFlag(), null));
            mockEventSource.RaiseMessageRcvd(eventArgs);

            // verify flag was deleted
            Assert.Null(mockFlagCacheMgr.FlagForUser("int-flag", user));
        }

        [Fact]
        public void DeleteDoesnotRemoveFeatureFlagIfVersionIsLower()
        {
            // before DELETE, fill in flags, test it's there
            MakeStartedStreamingDataSource();
            PUTMessageSentToProcessor();
            var intFlagFromPUT = mockFlagCacheMgr.FlagForUser("int-flag", user).value.AsInt;
            Assert.Equal(15, intFlagFromPUT);

            // DELETE int-flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent("delete", DeleteFlagWithLowerVersion(), null));
            mockEventSource.RaiseMessageRcvd(eventArgs);

            // verify flag was not deleted
            Assert.NotNull(mockFlagCacheMgr.FlagForUser("int-flag", user));
        }

        [Fact]
        public async void PingCausesPoll()
        {
            MakeStartedStreamingDataSource();
            mockEventSource.RaiseMessageRcvd(new MessageReceivedEventArgs(new MessageEvent("ping", null, null)));
            var deadline = DateTime.Now.Add(TimeSpan.FromSeconds(5));
            while (DateTime.Now < deadline)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10));
                if (mockFlagCacheMgr.FlagsForUser(user).Count > 0)
                {
                    Assert.Equal(15, mockFlagCacheMgr.FlagForUser("int-flag", user).value.AsInt);
                    return;
                }
            }
            Assert.True(false, "timed out waiting for polled flags");
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

        string UpdatedFlagWithLowerVersion()
        {
            var updatedFlagAsJson = "{\"key\":\"int-flag\",\"version\":1,\"flagVersion\":192,\"value\":99,\"variation\":0,\"trackEvents\":false}";
            return updatedFlagAsJson;
        }

        string DeleteFlagWithLowerVersion()
        {
            var flagToDelete = "{\"key\":\"int-flag\",\"version\":1}";
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
