using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class FeatureFlagEventTests : BaseTest
    {
        [Fact]
        public void ReturnsFlagVersionAsVersion()
        {
            var flag = new FeatureFlagBuilder().FlagVersion(123).Version(456).Build();
            var flagEvent = new FeatureFlagEvent("my-flag", flag);
            Assert.Equal(123, flagEvent.EventVersion);
        }

        [Fact]
        public void FallsBackToVersionAsVersion()
        {
            var flag = new FeatureFlagBuilder().Version(456).Build();
            var flagEvent = new FeatureFlagEvent("my-flag", flag);
            Assert.Equal(456, flagEvent.EventVersion);
        }
    }
}
