using LaunchDarkly.Sdk.Xamarin.PlatformSpecific;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Xamarin
{
    // The DefaultDeviceInfo functionality is also tested by LdClientEndToEndTests.InitWithKeylessAnonUserAddsKeyAndReusesIt(),
    // which is a more realistic test since it uses a full client instance. However, currently LdClientEndToEndTests can't be
    // run on every platform, so we'll also test the lower-level logic here.
    public class DefaultDeviceInfoTests : BaseTest
    {
        public DefaultDeviceInfoTests(ITestOutputHelper testOutput) : base(testOutput) { }

        [Fact]
        public void UniqueDeviceIdGeneratesStableValue()
        {
            // Note, on mobile platforms, the generated user key is the device ID and is stable; on other platforms,
            // it's a GUID that is cached in local storage. Calling ClearCachedClientId() resets the latter.
            ClientIdentifier.ClearCachedClientId(testLogger);

            var ddi = new DefaultDeviceInfo(testLogger);
            var id0 = ddi.UniqueDeviceId();
            Assert.NotNull(id0);

            var id1 = ddi.UniqueDeviceId();
            Assert.Equal(id0, id1);
        }
    }
}
