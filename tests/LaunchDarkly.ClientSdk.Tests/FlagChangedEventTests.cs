using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.TestHelpers;
using Xunit;
using Xunit.Abstractions;

using static LaunchDarkly.Sdk.Client.TestUtil;

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
            var listener1 = new EventSink<FlagChangedEventArgs>();
            var listener2 = new EventSink<FlagChangedEventArgs>();
            manager.FlagChanged += listener1.Add;
            manager.FlagChanged += listener2.Add;

            manager.FireEvent(new FlagChangedEventArgs(INT_FLAG, LdValue.Of(7), LdValue.Of(6), false));
            var event1a = listener1.ExpectValue();
            var event2a = listener2.ExpectValue();

            manager.FireEvent(new FlagChangedEventArgs(DOUBLE_FLAG, LdValue.Of(10.5f), LdValue.Of(9.5f), false));
            var event1b = listener1.ExpectValue();
            var event2b = listener2.ExpectValue();

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
            var listener1 = new EventSink<FlagChangedEventArgs>();
            var listener2 = new EventSink<FlagChangedEventArgs>();
            manager.FlagChanged += listener1.Add;
            manager.FlagChanged += listener2.Add;

            manager.FlagChanged -= listener1.Add;

            manager.FireEvent(new FlagChangedEventArgs(INT_FLAG, LdValue.Of(7), LdValue.Of(6), false));

            var e = listener2.ExpectValue();
            Assert.Equal(INT_FLAG, e.Key);
            Assert.Equal(7, e.NewValue.AsInt);
            Assert.Equal(6, e.OldValue.AsInt);

            listener1.ExpectNoValue();
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
            var called = new AtomicBoolean(false);

            manager.FlagChanged += (sender, args) =>
            {
                lock (locker)
                {
                    called.GetAndSet(true);
                }
            };

            lock (locker)
            {
                manager.FireEvent(new FlagChangedEventArgs(INT_FLAG, LdValue.Of(2), LdValue.Of(1), false));
                Assert.False(called.Get());
            }
        }
    }
}
