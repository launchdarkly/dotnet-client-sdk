using System.Threading;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.TestHelpers;
using Foundation;
using Xunit;

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
            var config = BasicConfig().Build();
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
            var config = BasicConfig()
                .DeviceInfo(null).Build();
            using (var client = TestUtil.CreateClient(config, anonUser))
            {
                var user = client.User;
                Assert.NotNull(user.Key);
                Assert.NotEqual("", user.Key);
            }
        }

        [Fact]
        public void EventHandlerIsCalledOnUIThread()
        {
            var td = TestData.DataSource();
            var config = BasicConfig().DataSource(td).Build();

            var captureMainThread = new EventSink<Thread>();
            NSRunLoop.Main.BeginInvokeOnMainThread(() => captureMainThread.Enqueue(Thread.CurrentThread));
            var mainThread = captureMainThread.ExpectValue();

            using (var client = TestUtil.CreateClient(config, BasicUser))
            {
                var receivedOnThread = new EventSink<Thread>();
                client.FlagTracker.FlagValueChanged += (sender, args) =>
                    receivedOnThread.Enqueue(Thread.CurrentThread);

                td.Update(td.Flag("flagkey").Variation(true));

                var t = receivedOnThread.ExpectValue();
                Assert.Equal(mainThread, t);
            }
        }
    }
}
