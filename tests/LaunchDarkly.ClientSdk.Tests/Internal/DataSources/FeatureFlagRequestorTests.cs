using System;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Json;
using LaunchDarkly.Sdk.Client.Subsystems;
using LaunchDarkly.TestHelpers.HttpTest;
using Xunit;
using Xunit.Abstractions;

using static LaunchDarkly.TestHelpers.JsonAssertions;

namespace LaunchDarkly.Sdk.Client.Internal.DataSources
{
    // End-to-end tests of this component against an embedded HTTP server. This is covered
    // in more detail by PollingDataSourceTest, but FeatureFlagRequestor can also be used from
    // StreamingDataSource.

    public class FeatureFlagRequestorTests : BaseTest
    {
        public FeatureFlagRequestorTests(ITestOutputHelper testOutput) : base(testOutput) { }

        private const string _mobileKey = "FAKE_KEY";

        // User key constructed to test base64 encoding that differs between the standard and "URL and Filename safe"
        // base64 encodings from RFC4648. We need to use the URL safe encoding for flag requests.
        private static readonly Context _context = Context.New("foo_bar__?");
        // Note that in a real use case, the user encoding may vary depending on the target platform, because the SDK adds custom
        // user attributes like "os". But the lower-level FeatureFlagRequestor component does not do that.

        private const string _allDataJson = "{}"; // Note that in this implementation, unlike the .NET SDK, FeatureFlagRequestor does not unmarshal the response

        [Theory]
        [InlineData("", false, "/msdk/evalx/users/", "")]
        [InlineData("", true, "/msdk/evalx/users/", "?withReasons=true")]
        [InlineData("/basepath", false, "/basepath/msdk/evalx/users/", "")]
        [InlineData("/basepath", true, "/basepath/msdk/evalx/users/", "?withReasons=true")]
        [InlineData("/basepath/", false, "/basepath/msdk/evalx/users/", "")]
        [InlineData("/basepath/", true, "/basepath/msdk/evalx/users/", "?withReasons=true")]
        public async Task GetFlagsUsesCorrectUriAndMethodInGetModeAsync(
            string baseUriExtraPath,
            bool withReasons,
            string expectedPathWithoutUser,
            string expectedQuery
            )
        {
            using (var server = HttpServer.Start(Handlers.BodyJson(_allDataJson)))
            {
                var baseUri = new Uri(server.Uri.ToString().TrimEnd('/') + baseUriExtraPath);

                var config = Configuration.Default(_mobileKey);

                using (var requestor = new FeatureFlagRequestor(
                    baseUri,
                    _context,
                    withReasons,
                    new LdClientContext(config).Http,
                    testLogger))
                {
                    var resp = await requestor.FeatureFlagsAsync();
                    Assert.Equal(200, resp.statusCode);
                    Assert.Equal(_allDataJson, resp.jsonResponse);

                    var req = server.Recorder.RequireRequest();
                    Assert.Equal("GET", req.Method);
                    AssertHelpers.ContextsEqual(_context, TestUtil.Base64ContextFromUrlPath(req.Path, expectedPathWithoutUser));
                    Assert.Equal(expectedQuery, req.Query);
                    Assert.Equal(_mobileKey, req.Headers["Authorization"]);
                    Assert.Equal("", req.Body);
                }
            }
        }

        // REPORT mode is known to fail in Android (ch47341)
#if !__ANDROID__
        [Theory]
        [InlineData("", false, "/msdk/evalx/user", "")]
        [InlineData("", true, "/msdk/evalx/user", "?withReasons=true")]
        [InlineData("/basepath", false, "/basepath/msdk/evalx/user", "")]
        [InlineData("/basepath", true, "/basepath/msdk/evalx/user", "?withReasons=true")]
        [InlineData("/basepath/", false, "/basepath/msdk/evalx/user", "")]
        [InlineData("/basepath/", true, "/basepath/msdk/evalx/user", "?withReasons=true")]
        public async Task GetFlagsUsesCorrectUriAndMethodInReportModeAsync(
            string baseUriExtraPath,
            bool withReasons,
            string expectedPath,
            string expectedQuery
            )
        {
            using (var server = HttpServer.Start(Handlers.BodyJson(_allDataJson)))
            {
                var baseUri = new Uri(server.Uri.ToString().TrimEnd('/') + baseUriExtraPath);

                var config = Configuration.Builder(_mobileKey)
                    .Http(Components.HttpConfiguration().UseReport(true))
                    .Build();

                using (var requestor = new FeatureFlagRequestor(
                    baseUri,
                    _context,
                    withReasons,
                    new LdClientContext(config).Http,
                    testLogger))
                {
                    var resp = await requestor.FeatureFlagsAsync();
                    Assert.Equal(200, resp.statusCode);
                    Assert.Equal(_allDataJson, resp.jsonResponse);

                    var req = server.Recorder.RequireRequest();
                    Assert.Equal("REPORT", req.Method);
                    Assert.Equal(expectedPath, req.Path);
                    Assert.Equal(expectedQuery, req.Query);
                    Assert.Equal(_mobileKey, req.Headers["Authorization"]);
                    AssertJsonEqual(LdJsonSerialization.SerializeObject(_context), req.Body);
                }
            }
        }
#endif
    }
}
