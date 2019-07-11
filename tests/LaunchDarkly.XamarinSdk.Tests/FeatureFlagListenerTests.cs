using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class FeatureFlagListenerTests : BaseTest
    {
        private const string INT_FLAG = "int-flag";
        private const string DOUBLE_FLAG = "double-flag";

        FeatureFlagListenerManager Manager()
        {
            return new FeatureFlagListenerManager();
        }

        TestListener Listener(int expectedTimes)
        {
            return new TestListener(expectedTimes);
        }

        [Fact]
        public void CanRegisterListeners()
        {
            var manager = Manager();
            var listener1 = Listener(2);
            var listener2 = Listener(2);
            manager.RegisterListener(listener1, INT_FLAG);
            manager.RegisterListener(listener1, DOUBLE_FLAG);
            manager.RegisterListener(listener2, INT_FLAG);
            manager.RegisterListener(listener2, DOUBLE_FLAG);

            manager.FlagWasUpdated(INT_FLAG, 7);
            manager.FlagWasUpdated(DOUBLE_FLAG, 10.5);
            listener1.Countdown.Wait();
            listener2.Countdown.Wait();

            Assert.Equal(7, listener1.FeatureFlags[INT_FLAG]);
            Assert.Equal(10.5, listener1.FeatureFlags[DOUBLE_FLAG]);
            Assert.Equal(7, listener2.FeatureFlags[INT_FLAG]);
            Assert.Equal(10.5, listener2.FeatureFlags[DOUBLE_FLAG]);
        }

        [Fact]
        public void CanUnregisterListeners()
        {
            var manager = Manager();
            var listener1 = Listener(2);
            var listener2 = Listener(2);
            manager.RegisterListener(listener1, INT_FLAG);
            manager.RegisterListener(listener1, DOUBLE_FLAG);
            manager.RegisterListener(listener2, INT_FLAG);
            manager.RegisterListener(listener2, DOUBLE_FLAG);

            manager.FlagWasUpdated(INT_FLAG, 7);
            manager.FlagWasUpdated(DOUBLE_FLAG, 10.5);
            listener1.Countdown.Wait();
            listener2.Countdown.Wait();

            Assert.Equal(7, listener1.FeatureFlags[INT_FLAG]);
            Assert.Equal(10.5, listener1.FeatureFlags[DOUBLE_FLAG]);
            Assert.Equal(7, listener2.FeatureFlags[INT_FLAG]);
            Assert.Equal(10.5, listener2.FeatureFlags[DOUBLE_FLAG]);

            manager.UnregisterListener(listener1, INT_FLAG);
            manager.UnregisterListener(listener2, INT_FLAG);
            manager.UnregisterListener(listener1, DOUBLE_FLAG);
            manager.UnregisterListener(listener2, DOUBLE_FLAG);
            listener1.Reset();
            listener2.Reset();
            manager.FlagWasUpdated(INT_FLAG, 2);
            manager.FlagWasUpdated(DOUBLE_FLAG, 12.5);

            // This is pretty hacky, but since we're testing for the *lack* of a call, there's no signal we can wait on.
            Thread.Sleep(100);

            Assert.NotEqual(2, listener1.FeatureFlags[INT_FLAG]);
            Assert.NotEqual(12.5, listener1.FeatureFlags[DOUBLE_FLAG]);
            Assert.NotEqual(2, listener2.FeatureFlags[INT_FLAG]);
            Assert.NotEqual(12.5, listener2.FeatureFlags[DOUBLE_FLAG]);
        }

        [Fact]
        public void ListenerGetsUpdatedFlagValues()
        {
            var manager = Manager();
            var listener1 = Listener(1);
            var listener2 = Listener(1);
            manager.RegisterListener(listener1, INT_FLAG);
            manager.RegisterListener(listener2, INT_FLAG);

            manager.FlagWasUpdated(INT_FLAG, JToken.FromObject(99));
            listener1.Countdown.Wait();
            listener2.Countdown.Wait();

            Assert.Equal(99, listener1.FeatureFlags[INT_FLAG].Value<int>());
            Assert.Equal(99, listener2.FeatureFlags[INT_FLAG].Value<int>());
        }

        [Fact]
        public void ListenerGetsUpdatedWhenManagerFlagDeleted()
        {
            var manager = Manager();
            var listener = Listener(1);
            manager.RegisterListener(listener, INT_FLAG);

            manager.FlagWasUpdated(INT_FLAG, 2);
            listener.Countdown.Wait();
            Assert.True(listener.FeatureFlags.ContainsKey(INT_FLAG));

            listener.Reset();
            manager.FlagWasDeleted(INT_FLAG);
            listener.Countdown.Wait();

            Assert.False(listener.FeatureFlags.ContainsKey(INT_FLAG));
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
            var listener = new TestDeadlockListener(locker);

            manager.RegisterListener(listener, INT_FLAG);
            lock (locker)
            {
                manager.FlagWasUpdated(INT_FLAG, 2);
                Assert.False(listener.Called);
            }
        }
    }

    public class TestListener : IFeatureFlagListener
    {
        private readonly int ExpectedCalls;
        public CountdownEvent Countdown;

        public TestListener(int expectedCalls)
        {
            ExpectedCalls = expectedCalls;
            Reset();
        }

        IDictionary<string, JToken> featureFlags = new Dictionary<string, JToken>();
        public IDictionary<string, JToken> FeatureFlags
        {
            get
            {
                return featureFlags;
            }
        }

        public void FeatureFlagChanged(string featureFlagKey, JToken value)
        {
            featureFlags[featureFlagKey] = value;
            Countdown.Signal();
        }

        public void FeatureFlagDeleted(string featureFlagKey)
        {
            featureFlags.Remove(featureFlagKey);
            Countdown.Signal();
        }

        public void Reset()
        {
            Countdown = new CountdownEvent(ExpectedCalls);
        }
    }

    public class TestDeadlockListener : IFeatureFlagListener
    {
        private readonly object _locker;
        public volatile bool Called;

        public TestDeadlockListener(object locker)
        {
            _locker = locker;
        }

        public void FeatureFlagChanged(string featureFlagKey, JToken value)
        {
            lock(_locker)
            {
                Called = true;
            }
        }

        public void FeatureFlagDeleted(string featureFlagKey)
        {
            lock(_locker)
            {
                Called = true;
            }
        }
    }
}
