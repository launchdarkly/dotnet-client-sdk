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
            var baseUser = Context.New("key");
            var config = BasicConfig().Build();
            using (var client = TestUtil.CreateClient(config, baseUser))
            {
                var context = client.Context;
                Assert.Equal(baseUser.Key, context.Key);
                Assert.StartsWith("Android ", context.GetValue("os").AsString);
                Assert.NotEqual(LdValue.Null, context.GetValue("device"));
            }
        }

        [Fact]
        public void CanGetUniqueUserKey()
        {
            var anonUser = Context.Builder(Internal.Constants.AutoKeyMagicValue).Transient(true).Build();
            var config = BasicConfig()
                .DeviceInfo(null).Build();
            using (var client = TestUtil.CreateClient(config, anonUser))
            {
                var context = client.Context;
                Assert.NotNull(context.Key);
                Assert.NotEqual("", context.Key);
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
