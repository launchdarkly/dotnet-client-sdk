﻿using Xunit;

namespace LaunchDarkly.Sdk.Client.iOS.Tests
{
    public class IOsSpecificTests : BaseTest
    {
        [Fact]
        public void SdkReturnsIOsPlatformType()
        {
            Assert.Equal(PlatformType.IOs, LdClient.PlatformType);
        }

        [Fact]
        public void UserHasOSAndDeviceAttributesForPlatform()
        {
            var baseUser = User.WithKey("key");
            var config = TestUtil.TestConfig("mobileKey").Build();
            using (var client = TestUtil.CreateClient(config, baseUser))
            {
                var user = client.User;
                Assert.Equal(baseUser.Key, user.Key);
                Assert.Contains("os", user.Custom.Keys);
                Assert.StartsWith("iOS ", user.Custom["os"].AsString);
                Assert.Contains("device", user.Custom.Keys);
            }
        }

        [Fact]
        public void CanGetUniqueUserKey()
        {
            var anonUser = User.Builder((string)null).Anonymous(true).Build();
            var config = TestUtil.TestConfig("mobileKey")
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