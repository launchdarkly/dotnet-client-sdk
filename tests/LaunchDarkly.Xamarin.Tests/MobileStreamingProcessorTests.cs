using System;
using System.Collections.Generic;
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

        User user = User.WithKey("user key");
        EventSourceMock mockEventSource;
        TestEventSourceFactory eventSourceFactory;
        IFlagCacheManager mockFlagCacheMgr;

        private IMobileUpdateProcessor MobileStreamingProcessorStarted()
        {
            mockEventSource = new EventSourceMock();
            eventSourceFactory = new TestEventSourceFactory(mockEventSource);
            // stub with an empty InMemoryCache, so Stream updates can be tested
            mockFlagCacheMgr = new MockFlagCacheManager(new UserFlagInMemoryCache());
            var config = Configuration.Default("someKey")
                                      .WithConnectionManager(new MockConnectionManager(true))
                                      .WithIsStreamingEnabled(true)
                                      .WithFlagCacheManager(mockFlagCacheMgr);
            
            var processor = Factory.CreateUpdateProcessor(config, user, mockFlagCacheMgr, eventSourceFactory.Create());
            processor.Start();
            return processor;
        }

        [Fact]
        public void CanCreateMobileStreamingProcFromFactory()
        {
            var streamingProcessor = MobileStreamingProcessorStarted();
            Assert.IsType<MobileStreamingProcessor>(streamingProcessor);
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

        public event EventHandler<StateChangedEventArgs> Opened;
        public event EventHandler<StateChangedEventArgs> Closed;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<CommentReceivedEventArgs> CommentReceived;
        public event EventHandler<ExceptionEventArgs> Error;

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
