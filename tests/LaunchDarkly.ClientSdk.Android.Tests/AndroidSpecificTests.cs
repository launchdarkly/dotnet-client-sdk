using System.Threading;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.TestHelpers;
using Android.OS;
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
            var config = BasicConfig().Build();
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
            new Handler(Looper.MainLooper).Post(() => captureMainThread.Enqueue(Thread.CurrentThread));
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
