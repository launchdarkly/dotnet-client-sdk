using System;
using Common.Logging;
using LaunchDarkly.Client;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Xamarin.Tests
{
    [Collection("serialize all tests")]
    public class BaseTest : IDisposable
    {
        public BaseTest()
        {
            LogManager.Adapter = new LogSinkFactoryAdapter();
            TestUtil.ClearClient();
        }

        public BaseTest(ITestOutputHelper testOutput)
        {
            LogManager.Adapter = new LogSinkFactoryAdapter(testOutput.WriteLine);
            TestUtil.ClearClient();
        }

        public void Dispose()
        {
            TestUtil.ClearClient();
        }

        protected void ClearCachedFlags(User user)
        {
            PlatformSpecific.Preferences.Clear(Constants.FLAGS_KEY_PREFIX + user.Key);
        }
    }
}
