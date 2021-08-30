using System.Collections.Concurrent;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client
{
    public class FlagChangedEventTests : BaseTest
    {
        private const string INT_FLAG = "int-flag";
        private const string DOUBLE_FLAG = "double-flag";

        public FlagChangedEventTests(ITestOutputHelper testOutput) : base(testOutput) { }

        FlagChangedEventManager Manager()
        {
            return new FlagChangedEventManager(testLogger);
        }

        [Fact]
        public void CanRegisterListeners()
        {
            var manager = Manager();
            var listener1 = new FlagChangedEventSink();
            var listener2 = new FlagChangedEventSink();
            manager.FlagChanged += listener1.Handler;
            manager.FlagChanged += listener2.Handler;

            manager.FlagWasUpdated(INT_FLAG, LdValue.Of(7), LdValue.Of(6));
            manager.FlagWasUpdated(DOUBLE_FLAG, LdValue.Of(10.5f), LdValue.Of(9.5f));

            var event1a = listener1.Await();
            var event1b = listener1.Await();
            var event2a = listener2.Await();
            var event2b = listener2.Await();

            Assert.Equal(INT_FLAG, event1a.Key);
            Assert.Equal(INT_FLAG, event2a.Key);
            Assert.Equal(7, event1a.NewValue.AsInt);
            Assert.Equal(7, event2a.NewValue.AsInt);
            Assert.Equal(6, event1a.OldValue.AsInt);
            Assert.Equal(6, event2a.OldValue.AsInt);
            Assert.False(event1a.FlagWasDeleted);
            Assert.False(event2a.FlagWasDeleted);

            Assert.Equal(DOUBLE_FLAG, event1b.Key);
            Assert.Equal(DOUBLE_FLAG, event2b.Key);
            Assert.Equal(10.5, event1b.NewValue.AsFloat);
            Assert.Equal(10.5, event2b.NewValue.AsFloat);
            Assert.Equal(9.5, event1b.OldValue.AsFloat);
            Assert.Equal(9.5, event2b.OldValue.AsFloat);
            Assert.False(event1b.FlagWasDeleted);
            Assert.False(event2b.FlagWasDeleted);
        }

        [Fact]
        public void CanUnregisterListeners()
        {
            var manager = Manager();
            var listener1 = new FlagChangedEventSink();
            var listener2 = new FlagChangedEventSink();
            manager.FlagChanged += listener1.Handler;
            manager.FlagChanged += listener2.Handler;

            manager.FlagChanged -= listener1.Handler;

            manager.FlagWasUpdated(INT_FLAG, LdValue.Of(7), LdValue.Of(6));

            var e = listener2.Await();
            Assert.Equal(INT_FLAG, e.Key);
            Assert.Equal(7, e.NewValue.AsInt);
            Assert.Equal(6, e.OldValue.AsInt);

            // This is pretty hacky, but since we're testing for the *lack* of a call, there's no signal we can wait on.
            Thread.Sleep(100);

            Assert.True(listener1.IsEmpty);
        }

        [Fact]
        public void ListenerGetsUpdatedWhenManagerFlagDeleted()
        {
            var manager = Manager();
            var listener = new FlagChangedEventSink();
            manager.FlagChanged += listener.Handler;

            manager.FlagWasDeleted(INT_FLAG, LdValue.Of(1));

            var e = listener.Await();
            Assert.Equal(INT_FLAG, e.Key);
            Assert.Equal(1, e.OldValue.AsInt);
            Assert.True(e.FlagWasDeleted);
        }

        [Fact]
        public void ListenerCallIsDeferred()
        {
            // This verifies that we are not making synchronous calls to listeners, so they cannot deadlock by trying to
            // acquire some resource that is being held by the caller. There are three possible things that can happen:
            // 1. We call the listener synchronously; listener.Called gets set to true before FlagWasUpdated returns. Fail.
            // 2. The listener is queued somewhere for later execution, so it doesn't even start to run before the end of
            //    the test. Pass.
            // 3. The listener starts executing immediately on another thread; that's OK too, because the lock(locker) block
            //    ensures it won't set Called until after we have checked it. Pass.
            var manager = Manager();
            var locker = new object();
            var called = false;

            manager.FlagChanged += (sender, args) =>
            {
                lock (locker)
                {
                    Volatile.Write(ref called, true);
                }
            };

            lock (locker)
            {
                manager.FlagWasUpdated(INT_FLAG, LdValue.Of(2), LdValue.Of(1));
                Assert.False(Volatile.Read(ref called));
            }
        }
    }

    public class FlagChangedEventSink
    {
        private BlockingCollection<FlagChangedEventArgs> _events = new BlockingCollection<FlagChangedEventArgs>();

        public void Handler(object sender, FlagChangedEventArgs args)
        {
            _events.Add(args);
        }

        public FlagChangedEventArgs Await()
        {
            return _events.Take();
        }

        public bool IsEmpty => _events.Count == 0;
    }
}
