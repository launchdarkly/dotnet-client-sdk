using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LaunchDarkly.Client;
using LaunchDarkly.Common;
using LaunchDarkly.EventSource;
using Newtonsoft.Json;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class MobileStreamingProcessorTests
    {
        private const string initialFlagsJson = "{" +
            "\"int-flag\":{\"value\":15,\"version\":100}," +
            "\"float-flag\":{\"value\":13.5,\"version\":100}," +
            "\"string-flag\":{\"value\":\"markw@magenic.com\",\"version\":100}" +
            "}";

        private readonly User user = User.WithKey("me");
        private const string encodedUser = "eyJrZXkiOiJtZSIsImN1c3RvbSI6e319";

        private EventSourceMock mockEventSource;
        private TestEventSourceFactory eventSourceFactory;
        private IFlagCacheManager mockFlagCacheMgr;
        private Configuration config;

        public MobileStreamingProcessorTests()
        {
            mockEventSource = new EventSourceMock();
            eventSourceFactory = new TestEventSourceFactory(mockEventSource);
            mockFlagCacheMgr = new MockFlagCacheManager(new UserFlagInMemoryCache());
            config = Configuration.Default("someKey")
                                  .WithConnectionManager(new MockConnectionManager(true))
                                  .WithIsStreamingEnabled(true)
                                  .WithFlagCacheManager(mockFlagCacheMgr);

        }

        private IMobileUpdateProcessor MobileStreamingProcessorStarted()
        {
            IMobileUpdateProcessor processor = new MobileStreamingProcessor(config, mockFlagCacheMgr, user, eventSourceFactory.Create());
            processor.Start();
            return processor;
        }

        [Fact]
        public void StreamUriInGetModeHasUser()
        {
            config.WithUseReport(false);
            var streamingProcessor = MobileStreamingProcessorStarted();
            var props = eventSourceFactory.ReceivedProperties;
            Assert.Equal(HttpMethod.Get, props.Method);
            Assert.Equal(new Uri(config.StreamUri, Constants.STREAM_REQUEST_PATH + encodedUser), props.StreamUri);
        }

        [Fact]
        public void StreamUriInGetModeHasReasonsParameterIfConfigured()
        {
            config.WithUseReport(false);
            config.WithEvaluationReasons(true);
            var streamingProcessor = MobileStreamingProcessorStarted();
            var props = eventSourceFactory.ReceivedProperties;
            Assert.Equal(new Uri(config.StreamUri, Constants.STREAM_REQUEST_PATH + encodedUser + "?withReasons=true"), props.StreamUri);
        }

        [Fact]
        public void StreamUriInReportModeHasNoUser()
        {
            config.WithUseReport(true);
            var streamingProcessor = MobileStreamingProcessorStarted();
            var props = eventSourceFactory.ReceivedProperties;
            Assert.Equal(new HttpMethod("REPORT"), props.Method);
            Assert.Equal(new Uri(config.StreamUri, Constants.STREAM_REQUEST_PATH), props.StreamUri);
        }

        [Fact]
        public void StreamUriInReportModeHasReasonsParameterIfConfigured()
        {
            config.WithUseReport(true);
            config.WithEvaluationReasons(true);
            var streamingProcessor = MobileStreamingProcessorStarted();
            var props = eventSourceFactory.ReceivedProperties;
            Assert.Equal(new Uri(config.StreamUri, Constants.STREAM_REQUEST_PATH + "?withReasons=true"), props.StreamUri);
        }

        [Fact]
        public async void StreamRequestBodyInReportModeHasUser()
        {
            config.WithUseReport(true);
            var streamingProcessor = MobileStreamingProcessorStarted();
            var props = eventSourceFactory.ReceivedProperties;
            var body = Assert.IsType<StringContent>(props.RequestBody);
            var s = await body.ReadAsStringAsync();
            Assert.Equal(user.AsJson(), s);
        }

        [Fact]
        public void PUTstoresFeatureFlags()
        {
            var streamingProcessor = MobileStreamingProcessorStarted();
            // should be empty before PUT message arrives
            var flagsInCache = mockFlagCacheMgr.FlagsForUser(user);
            Assert.Empty(flagsInCache);

            PUTMessageSentToProcessor();
            flagsInCache = mockFlagCacheMgr.FlagsForUser(user);
            Assert.NotEmpty(flagsInCache);
            int intFlagValue = mockFlagCacheMgr.FlagForUser("int-flag", user).value.ToObject<int>();
            Assert.Equal(15, intFlagValue);
        }

        [Fact]
        public void PATCHupdatesFeatureFlag()
        {
            // before PATCH, fill in flags
            var streamingProcessor = MobileStreamingProcessorStarted();
            PUTMessageSentToProcessor();
            var intFlagFromPUT = mockFlagCacheMgr.FlagForUser("int-flag", user).value.ToObject<int>();
            Assert.Equal(15, intFlagFromPUT);

            //PATCH to update 1 flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent(UpdatedFlag(), null), "patch");
            mockEventSource.RaiseMessageRcvd(eventArgs);

            //verify flag has changed
            int flagFromPatch = mockFlagCacheMgr.FlagForUser("int-flag", user).value.ToObject<int>();
            Assert.Equal(99, flagFromPatch);
        }

        [Fact]
        public void PATCHdoesnotUpdateFlagIfVersionIsLower()
        {
            // before PATCH, fill in flags
            var streamingProcessor = MobileStreamingProcessorStarted();
            PUTMessageSentToProcessor();
            var intFlagFromPUT = mockFlagCacheMgr.FlagForUser("int-flag", user).value.ToObject<int>();
            Assert.Equal(15, intFlagFromPUT);

            //PATCH to update 1 flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent(UpdatedFlagWithLowerVersion(), null), "patch");
            mockEventSource.RaiseMessageRcvd(eventArgs);

            //verify flag has not changed
            int flagFromPatch = mockFlagCacheMgr.FlagForUser("int-flag", user).value.ToObject<int>();
            Assert.Equal(15, flagFromPatch);
        }

        [Fact]
        public void DELETEremovesFeatureFlag()
        {
            // before DELETE, fill in flags, test it's there
            var streamingProcessor = MobileStreamingProcessorStarted();
            PUTMessageSentToProcessor();
            var intFlagFromPUT = mockFlagCacheMgr.FlagForUser("int-flag", user).value.ToObject<int>();
            Assert.Equal(15, intFlagFromPUT);

            // DELETE int-flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent(DeleteFlag(), null), "delete");
            mockEventSource.RaiseMessageRcvd(eventArgs);

            // verify flag was deleted
            Assert.Null(mockFlagCacheMgr.FlagForUser("int-flag", user));
        }

        [Fact]
        public void DELTEdoesnotRemoveFeatureFlagIfVersionIsLower()
        {
            // before DELETE, fill in flags, test it's there
            var streamingProcessor = MobileStreamingProcessorStarted();
            PUTMessageSentToProcessor();
            var intFlagFromPUT = mockFlagCacheMgr.FlagForUser("int-flag", user).value.ToObject<int>();
            Assert.Equal(15, intFlagFromPUT);

            // DELETE int-flag
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent(DeleteFlagWithLowerVersion(), null), "delete");
            mockEventSource.RaiseMessageRcvd(eventArgs);

            // verify flag was not deleted
            Assert.NotNull(mockFlagCacheMgr.FlagForUser("int-flag", user));
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
            MessageReceivedEventArgs eventArgs = new MessageReceivedEventArgs(new MessageEvent(initialFlagsJson, null), "put");
            mockEventSource.RaiseMessageRcvd(eventArgs);
        }
    }

    class TestEventSourceFactory
    {
        public StreamProperties ReceivedProperties { get; private set; }
        public IDictionary<string, string> ReceivedHeaders { get; private set; }
        IEventSource _eventSource;

        public TestEventSourceFactory(IEventSource eventSource)
        {
            _eventSource = eventSource;
        }

        public StreamManager.EventSourceCreator Create()
        {
            return (StreamProperties sp, IDictionary<string, string> headers) =>
            {
                ReceivedProperties = sp;
                ReceivedHeaders = headers;
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

        public void RaiseMessageRcvd(MessageReceivedEventArgs eventArgs)
        {
            MessageReceived(null, eventArgs);
        }
    }
}
