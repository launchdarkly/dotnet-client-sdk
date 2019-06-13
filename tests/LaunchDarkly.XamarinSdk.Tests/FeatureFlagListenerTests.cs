using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class FeatureFlagListenerTests
    {
        private const string INT_FLAG = "int-flag";
        private const string DOUBLE_FLAG = "double-flag";

        FeatureFlagListenerManager Manager()
        {
            return new FeatureFlagListenerManager();
        }

        TestListener Listener()
        {
            return new TestListener();
        }

        [Fact]
        public void CanRegisterListeners()
        {
            var manager = Manager();
            var listener1 = Listener();
            var listener2 = Listener();
            manager.RegisterListener(listener1, INT_FLAG);
            manager.RegisterListener(listener1, DOUBLE_FLAG);
            manager.RegisterListener(listener2, INT_FLAG);
            manager.RegisterListener(listener2, DOUBLE_FLAG);
            manager.FlagWasUpdated(INT_FLAG, 7);
            manager.FlagWasUpdated(DOUBLE_FLAG, 10.5);
            Assert.Equal(7, listener1.FeatureFlags[INT_FLAG]);
            Assert.Equal(10.5, listener1.FeatureFlags[DOUBLE_FLAG]);
            Assert.Equal(7, listener2.FeatureFlags[INT_FLAG]);
            Assert.Equal(10.5, listener2.FeatureFlags[DOUBLE_FLAG]);
        }

        [Fact]
        public void CanUnregisterListeners()
        {
            var manager = Manager();
            var listener1 = Listener();
            var listener2 = Listener();
            manager.RegisterListener(listener1, INT_FLAG);
            manager.RegisterListener(listener1, DOUBLE_FLAG);
            manager.RegisterListener(listener2, INT_FLAG);
            manager.RegisterListener(listener2, DOUBLE_FLAG);
            manager.FlagWasUpdated(INT_FLAG, 7);
            manager.FlagWasUpdated(DOUBLE_FLAG, 10.5);
            Assert.Equal(7, listener1.FeatureFlags[INT_FLAG]);
            Assert.Equal(10.5, listener1.FeatureFlags[DOUBLE_FLAG]);
            Assert.Equal(7, listener2.FeatureFlags[INT_FLAG]);
            Assert.Equal(10.5, listener2.FeatureFlags[DOUBLE_FLAG]);

            manager.UnregisterListener(listener1, INT_FLAG);
            manager.UnregisterListener(listener2, INT_FLAG);
            manager.UnregisterListener(listener1, DOUBLE_FLAG);
            manager.UnregisterListener(listener2, DOUBLE_FLAG);
            manager.FlagWasUpdated(INT_FLAG, 2);
            manager.FlagWasUpdated(DOUBLE_FLAG, 12.5);

            Assert.NotEqual(2, listener1.FeatureFlags[INT_FLAG]);
            Assert.NotEqual(12.5, listener1.FeatureFlags[DOUBLE_FLAG]);
            Assert.NotEqual(2, listener2.FeatureFlags[INT_FLAG]);
            Assert.NotEqual(12.5, listener2.FeatureFlags[DOUBLE_FLAG]);
        }

        [Fact]
        public void ListenerGetsUpdatedFlagValues()
        {
            var manager = Manager();
            var listener1 = Listener();
            var listener2 = Listener();
            manager.RegisterListener(listener1, INT_FLAG);
            manager.RegisterListener(listener2, INT_FLAG);

            manager.FlagWasUpdated(INT_FLAG, JToken.FromObject(99));

            Assert.Equal(99, listener1.FeatureFlags[INT_FLAG].Value<int>());
            Assert.Equal(99, listener2.FeatureFlags[INT_FLAG].Value<int>());
        }

        [Fact]
        public void ListenerGetsUpdatedWhenManagerFlagDeleted()
        {
            var manager = Manager();
            var listener = Listener();
            manager.RegisterListener(listener, INT_FLAG);
            manager.FlagWasUpdated(INT_FLAG, 2);
            Assert.True(listener.FeatureFlags.ContainsKey(INT_FLAG));
            manager.FlagWasDeleted(INT_FLAG);
            Assert.False(listener.FeatureFlags.ContainsKey(INT_FLAG));
        }
    }

    public class TestListener : IFeatureFlagListener
    {
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
        }

        public void FeatureFlagDeleted(string featureFlagKey)
        {
            featureFlags.Remove(featureFlagKey);
        }
    }
}
