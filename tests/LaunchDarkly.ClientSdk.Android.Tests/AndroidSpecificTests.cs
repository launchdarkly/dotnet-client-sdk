using Xunit;

namespace LaunchDarkly.Sdk.Client.Android.Tests
{
    public class AndroidSpecificTests : BaseTest
    {
        [Fact]
        public void SdkReturnsAndroidPlatformType()
        {
            Assert.Equal(PlatformType.Android, LdClient.PlatformType);
        }

        [Fact]
        public void UserHasOSAndDeviceAttributesForPlatform()
        {
            var baseUser = User.WithKey("key");
            var config = TestUtil.ConfigWithFlagsJson(baseUser, "mobileKey", "{}").Build();
            using (var client = TestUtil.CreateClient(config, baseUser))
            {
                var user = client.User;
                Assert.Equal(baseUser.Key, user.Key);
                Assert.Contains("os", user.Custom.Keys);
                Assert.StartsWith("Android ", user.Custom["os"].AsString);
                Assert.Contains("device", user.Custom.Keys);
            }
        }

        [Fact]
        public void CanGetUniqueUserKey()
        {
            var anonUser = User.Builder((string)null).Anonymous(true).Build();
            var config = TestUtil.ConfigWithFlagsJson(anonUser, "mobileKey", "{}")
                .DeviceInfo(null).Build();
            using (var client = TestUtil.CreateClient(config, anonUser))
            {
                var user = client.User;
                Assert.NotNull(user.Key);
                Assert.NotEqual("", user.Key);
            }
        }
    }
}
