using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Internal;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client
{
    [Collection("serialize all tests")]
    public class BaseTest : IDisposable
    {
        protected readonly ILogAdapter testLogging;
        protected readonly Logger testLogger;
        protected readonly LogCapture logCapture;

        public BaseTest()
        {
            logCapture = Logs.Capture();
            testLogging = logCapture;
            testLogger = logCapture.Logger("");
        }

        public BaseTest(ITestOutputHelper testOutput) : this()
        {
            testLogging = Logs.ToMultiple(TestLogging.TestOutputAdapter(testOutput), logCapture);
            testLogger = testLogging.Logger("");
        }

        protected void ClearCachedFlags(User user)
        {
            PlatformSpecific.Preferences.Clear(Constants.FLAGS_KEY_PREFIX + user.Key, testLogger);
        }

        public void Dispose()
        {
            TestUtil.ClearClient();
        }
    }
}
