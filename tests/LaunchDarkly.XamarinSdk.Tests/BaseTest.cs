using System;
using System.Threading.Tasks;
using Common.Logging;
using WireMock.Server;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    [Collection("serialize all tests")]
    public class BaseTest : IDisposable
    {
#if __ANDROID__
        // WireMock.Net currently doesn't work on Android, so we can't run tests that need an embedded HTTP server.
        // Mark such tests with [Fact(Skip = SkipIfCannotCreateHttpServer)] or [Theory(Skip = SkipIfCannotCreateHttpServer)]
        // and they'll be skipped on Android (but not on other platforms, because "Skip = null" is a no-op).
        // https://github.com/WireMock-Net/WireMock.Net/issues/292
        public const string SkipIfCannotCreateHttpServer = "can't run this test because we can't create an embedded HTTP server on this platform; see BaseTest.cs";
#else
        public const string SkipIfCannotCreateHttpServer = null;
#endif

        public BaseTest()
        {
            LogManager.Adapter = new LogSinkFactoryAdapter();
            TestUtil.ClearClient();
        }

        public void Dispose()
        {
            TestUtil.ClearClient();
        }

        protected void WithServer(Action<FluentMockServer> a)
        {
            var s = MakeServer();
            try
            {
                a(s);
            }
            finally
            {
                s.Stop();
            }
        }

        protected async Task WithServerAsync(Func<FluentMockServer, Task> a)
        {
            var s = MakeServer();
            try
            {
                await a(s);
            }
            finally
            {
                s.Stop();
            }
        }

        protected FluentMockServer MakeServer()
        {
#pragma warning disable RECS0110 // Condition is always 'true' or always 'false'
            if (SkipIfCannotCreateHttpServer != null)
            {
                // Until WireMock.Net supports all of our platforms, we'll need to mark any tests that use an embedded server
                // with [ConditionalFact(Condition = TestCondition.CanRunHttpServer)] or [ConditionalFact(Condition = TestCondition.CanRunHttpServer)]
                // instead of [Fact] or [Theory]; otherwise, you'll see this error.
                throw new Exception("tried to create an embedded HTTP server on a platform that doesn't support it; see BaseTest.cs");
            }
#pragma warning restore RECS0110 // Condition is always 'true' or always 'false'

            // currently we don't need to customize any server settings
#pragma warning disable CS0162 // Unreachable code detected
            return FluentMockServer.Start();
#pragma warning restore CS0162 // Unreachable code detected
        }
    }
}
