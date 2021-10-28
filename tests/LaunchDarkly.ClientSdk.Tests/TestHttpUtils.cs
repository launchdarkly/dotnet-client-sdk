using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Client.Integrations;
using LaunchDarkly.TestHelpers.HttpTest;
using Xunit;

namespace LaunchDarkly.Sdk.Client
{
    internal static class TestHttpUtils
    {
        // Used for TestWithSpecialHttpConfigurations
        internal class MessageHandlerThatAddsPathSuffix : HttpClientHandler
        {
            private readonly string _suffix;

            internal MessageHandlerThatAddsPathSuffix(string suffix) { _suffix = suffix; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                request.RequestUri = new Uri(request.RequestUri.ToString() + _suffix);
                return base.SendAsync(request, cancellationToken);
            }
        }

        // Used for TestWithSpecialHttpConfigurations
        public delegate void HttpConfigurationTestAction(Uri targetUri, HttpConfigurationBuilder httpConfig, HttpServer server);

        /// <summary>
        /// A test suite for all SDK components that support our standard HTTP configuration options.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Although all of our supported HTTP behaviors are implemented in shared code, there is no
        /// guarantee that all of our components are using that code, or using it correctly. So we
        /// should run this test suite on each component that can be affected by HttpConfigurationBuilder
        /// properties.
        /// </para>
        /// <para>
        /// For each HTTP configuration variant that is expected work (e.g., using a proxy server), it
        /// sets up a server that will produce whatever expected response was specified in
        /// <paramref name="responseHandler"/>. Then it runs <paramref name="testActionShouldSucceed"/>,
        /// which should create its component with the given configuration and base URI and verify that
        /// the component behaves correctly.
        /// </para>
        /// </remarks>
        /// <param name="responseHandler">specifies how the target server should response</param>
        /// <param name="testActionShouldSucceed">verifies the result of a test</param>
        /// <param name="log">the current TestLogger</param>
        public static void TestWithSpecialHttpConfigurations(
            Handler responseHandler,
            HttpConfigurationTestAction testActionShouldSucceed,
            Logger log
            )
        {
            log.Info("*** TestHttpClientCanUseCustomMessageHandler");
            TestHttpClientCanUseCustomMessageHandler(responseHandler, testActionShouldSucceed);

            log.Info("*** TestHttpClientCanUseProxy");
            TestHttpClientCanUseProxy(responseHandler, testActionShouldSucceed);
        }

        static void TestHttpClientCanUseCustomMessageHandler(Handler responseHandler,
            HttpConfigurationTestAction testActionShouldSucceed)
        {
            // To verify that a custom HttpMessageHandler will really be used if provided, we
            // create one that behaves normally except that it modifies the request path.
            // Then we verify that the server received a request with a modified path.

            var recordAndDelegate = Handlers.Record(out var recorder).Then(responseHandler);
            using (var server = HttpServer.Start(recordAndDelegate))
            {
                var suffix = "/modified-by-test";
                var messageHandler = new MessageHandlerThatAddsPathSuffix(suffix);
                var httpConfig = Components.HttpConfiguration().MessageHandler(messageHandler);

                testActionShouldSucceed(server.Uri, httpConfig, server);

                var request = recorder.RequireRequest();
                Assert.EndsWith(suffix, request.Path);
                recorder.RequireNoRequests(TimeSpan.FromMilliseconds(100));
            }
        }

        static void TestHttpClientCanUseProxy(Handler responseHandler,
            HttpConfigurationTestAction testActionShouldSucceed)
        {
            // To verify that a web proxy will really be used if provided, we set up a proxy
            // configuration pointing to our test server. It's not really a proxy server,
            // but if it receives a request that was intended for some other URI (instead of
            // the SDK trying to access that other URI directly), then that's a success.

            using (var server = HttpServer.Start(responseHandler))
            {
                var proxy = new WebProxy(server.Uri);
                var httpConfig = Components.HttpConfiguration().Proxy(proxy);
                var fakeBaseUri = new Uri("http://not-a-real-host");

                testActionShouldSucceed(fakeBaseUri, httpConfig, server);
            }
        }
    }
}
