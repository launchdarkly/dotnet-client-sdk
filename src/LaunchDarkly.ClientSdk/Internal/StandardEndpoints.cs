using System;

namespace LaunchDarkly.Sdk.Client.Internal
{
    internal static class StandardEndpoints
    {
        internal static Uri DefaultStreamingBaseUri = new Uri("https://clientstream.launchdarkly.com");
        internal static Uri DefaultPollingBaseUri = new Uri("https://clientsdk.launchdarkly.com");
        internal static Uri DefaultEventsBaseUri = new Uri("https://mobile.launchdarkly.com");

        internal static string StreamingGetRequestPath(string userDataBase64) =>
            "/meval/" + userDataBase64;
        internal const string StreamingReportRequestPath = "/meval";

        internal static string PollingRequestGetRequestPath(string userDataBase64) =>
            "msdk/evalx/users/" + userDataBase64;
        internal const string PollingRequestReportRequestPath = "msdk/evalx/user";

        internal const string AnalyticsEventsPostRequestPath = "mobile/events/bulk";
    }
}
