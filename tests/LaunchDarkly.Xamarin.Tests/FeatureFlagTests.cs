using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class FeatureFlagEventTests
    {
        [Fact]
        public void ReturnsFlagVersionAsVersion()
        {
            var flag = new FeatureFlag();
            flag.flagVersion = 123;
            flag.version = 456;
            var flagEvent = new FeatureFlagEvent("my-flag", flag);
            Assert.Equal(123, flagEvent.Version);
        }

        [Fact]
        public void FallsBackToVersionAsVersion()
        {
            var flag = new FeatureFlag();
            flag.version = 456;
            var flagEvent = new FeatureFlagEvent("my-flag", flag);
            Assert.Equal(456, flagEvent.Version);
        }
    }
}
