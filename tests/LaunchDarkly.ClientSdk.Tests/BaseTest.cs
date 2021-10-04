using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.Sdk.Client.Internal;
using LaunchDarkly.Sdk.Internal;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Client
{
    [Collection("serialize all tests")]
    public class BaseTest : IDisposable
    {
        protected const string BasicMobileKey = "mobile-key";
        protected static readonly User BasicUser = User.WithKey("user-key");

        protected readonly LoggingConfigurationBuilder testLogging;
        protected readonly Logger testLogger;
        protected readonly LogCapture logCapture = Logs.Capture();
        protected readonly TaskExecutor BasicTaskExecutor;

        public BaseTest() : this(capture => capture) { }

        public BaseTest(ITestOutputHelper testOutput) :
            this(capture => Logs.ToMultiple(TestLogging.TestOutputAdapter(testOutput), capture))
        { }

        protected BaseTest(Func<ILogAdapter, ILogAdapter> adapterFn)
        {
            var adapter = adapterFn(logCapture);
            testLogger = adapter.Level(LogLevel.Debug).Logger("");
            testLogging = Components.Logging(adapter).Level(LogLevel.Debug);
            BasicTaskExecutor = new TaskExecutor("test-sender", testLogger);
        }

        protected void ClearCachedFlags(User user)
        {
            PlatformSpecific.Preferences.Clear(Constants.FLAGS_KEY_PREFIX + user.Key, testLogger);
        }

        public void Dispose()
        {
            TestUtil.ClearClient();
        }

        protected ConfigurationBuilder BasicConfig() =>
            Configuration.Builder(BasicMobileKey)
                .DataSource(new MockDataSource().AsSingletonFactory())
                .Events(Components.NoEvents)
                .Logging(testLogging)
                .Persistence(Components.NoPersistence);  // unless we're specifically testing flag caching, this helps to prevent test state contamination
    }
}
