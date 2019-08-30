using System;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using WireMock.Logging;
using WireMock.Server;
using WireMock.Settings;
using Xunit;

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

        public void Dispose()
        {
            TestUtil.ClearClient();
        }

        protected void ClearCachedFlags(User user)
        {
            PlatformSpecific.Preferences.Clear(Constants.FLAGS_KEY_PREFIX + user.Key);
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
            // currently we don't need to customize any server settings
            var server = FluentMockServer.Start();

            // Perform an initial request to make sure the server has warmed up. On Android in particular, startup
            // of the very first server instance in the test run seems to be very slow, which may cause the first
            // request made by unit tests to time out.
            using (var client = new HttpClient())
            {
                AsyncUtils.WaitSafely(() => client.GetAsync(server.Urls[0]));
            }
            server.ResetLogEntries(); // so the initial request doesn't interfere with test postconditions
            return server;
        }
    }
}
