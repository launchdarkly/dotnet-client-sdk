using System;
using WireMock;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LaunchDarkly.Xamarin.Tests
{
    // Convenience methods to streamline the test code's usage of WireMock.
    public static class WireMockExtensions
    {
        public static string GetUrl(this FluentMockServer server)
        {
            return server.Urls[0];
        }

        public static FluentMockServer ForAllRequests(this FluentMockServer server, Func<IResponseBuilder, IResponseBuilder> builderFn)
        {
            server.Given(Request.Create()).RespondWith(builderFn(Response.Create().WithStatusCode(200)));
            return server;
        }

        public static IResponseBuilder WithJsonBody(this IResponseBuilder resp, string body)
        {
            return resp.WithBody(body).WithHeader("Content-Type", "application/json");
        }

        public static IResponseBuilder WithEventsBody(this IResponseBuilder resp, string body)
        {
            return resp.WithBody(body).WithHeader("Content-Type", "text/event-stream");
        }

        public static RequestMessage GetLastRequest(this FluentMockServer server)
        {
            foreach (LogEntry le in server.LogEntries)
            {
                return le.RequestMessage;
            }
            throw new InvalidOperationException("Did not receive a request");
        }
    }
}
