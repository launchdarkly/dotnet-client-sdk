using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class FeatureFlagEventTests : BaseTest
    {
        [Fact]
        public void ReturnsFlagVersionAsVersion()
        {
            var flag = new FeatureFlag();
            flag.flagVersion = 123;
            flag.version = 456;
            var flagEvent = new FeatureFlagEvent("my-flag", flag);
            Assert.Equal(123, flagEvent.EventVersion);
        }

        [Fact]
        public void FallsBackToVersionAsVersion()
        {
            var flag = new FeatureFlag();
            flag.version = 456;
            var flagEvent = new FeatureFlagEvent("my-flag", flag);
            Assert.Equal(456, flagEvent.EventVersion);
        }
    }
}
