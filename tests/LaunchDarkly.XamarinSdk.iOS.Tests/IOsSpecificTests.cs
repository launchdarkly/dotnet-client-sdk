using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    public class IOsSpecificTests
    {
        [Fact]
        public void UserHasOSAndDeviceAttributesForPlatform()
        {
            var baseUser = User.WithKey("key");
            var config = TestUtil.ConfigWithFlagsJson(baseUser, "mobileKey", "{}").build();
            using (var client = TestUtil.CreateClient(config, baseUser))
            {
                var user = client.User;
                Assert.Equal(baseUser.Key, user.Key);
                Assert.Contains("os", user.Custom.Keys);
                Assert.StartsWith("iOS ", user.Custom["os"].AsString);
                Assert.Contains("device", user.Custom.Keys);
            }
        }
    }
}
