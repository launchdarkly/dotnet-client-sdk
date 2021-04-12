using System;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Xamarin.HttpHelpers;
using Xunit;
using Xunit.Abstractions;

namespace LaunchDarkly.Sdk.Xamarin
{
    // End-to-end tests of this component against an embedded HTTP server.
    public class FeatureFlagRequestorTests : BaseTest
    {
        public FeatureFlagRequestorTests(ITestOutputHelper testOutput) : base(testOutput) { }

        private const string _mobileKey = "FAKE_KEY";

        // User key constructed to test base64 encoding that differs between the standard and "URL and Filename safe"
        // base64 encodings from RFC4648. We need to use the URL safe encoding for flag requests.
        private static readonly User _user = User.WithKey("foo_bar__?");
        private const string _encodedUser = "eyJrZXkiOiJmb29fYmFyX18_In0=";
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
            using (var server = TestHttpServer.Start(Handlers.JsonResponse("{}")))
            {
                var config = Configuration.Builder(_mobileKey)
                    .BaseUri(new Uri(server.Uri.ToString() + baseUriExtraPath))
                    .EvaluationReasons(withReasons)
                    .Build();

                using (var requestor = new FeatureFlagRequestor(config, _user, testLogger))
                {
                    var resp = await requestor.FeatureFlagsAsync();
                    Assert.Equal(200, resp.statusCode);
                    Assert.Equal(_allDataJson, resp.jsonResponse);

                    var req = server.Recorder.RequireRequest();
                    Assert.Equal("GET", req.Method);
                    Assert.Equal(expectedPathWithoutUser + _encodedUser, req.Path);
                    Assert.Equal(expectedQuery, req.Query);
                    Assert.Equal(_mobileKey, req.Headers["Authorization"]);
                    Assert.Null(req.Body);
                }
            }
        }

        // Report mode is currently disabled - ch47341
        //[Fact]
        //public async Task GetFlagsUsesCorrectUriAndMethodInReportModeAsync()
        //{
        //    await WithServerAsync(async server =>
        //    {
        //        server.ForAllRequests(r => r.WithJsonBody(_allDataJson));

        //        var config = Configuration.Builder(_mobileKey).BaseUri(new Uri(server.GetUrl()))
        //            .UseReport(true).Build();

        //        using (var requestor = new FeatureFlagRequestor(config, _user))
        //        {
        //            var resp = await requestor.FeatureFlagsAsync();
        //            Assert.Equal(200, resp.statusCode);
        //            Assert.Equal(_allDataJson, resp.jsonResponse);

        //            var req = server.GetLastRequest();
        //            Assert.Equal("REPORT", req.Method);
        //            Assert.Equal($"/msdk/evalx/user", req.Path);
        //            Assert.Equal("", req.RawQuery);
        //            Assert.Equal(_mobileKey, req.Headers["Authorization"][0]);
        //            TestUtil.AssertJsonEquals(LdValue.Parse(_userJson), TestUtil.NormalizeJsonUser(LdValue.Parse(req.Body)));
        //        }
        //    });
        //}

        //[Fact]
        //public async Task GetFlagsUsesCorrectUriAndMethodInReportModeWithReasonsAsync()
        //{
        //    await WithServerAsync(async server =>
        //    {
        //        server.ForAllRequests(r => r.WithJsonBody(_allDataJson));

        //        var config = Configuration.Builder(_mobileKey).BaseUri(new Uri(server.GetUrl()))
        //            .UseReport(true).EvaluationReasons(true).Build();

        //        using (var requestor = new FeatureFlagRequestor(config, _user))
        //        {
        //            var resp = await requestor.FeatureFlagsAsync();
        //            Assert.Equal(200, resp.statusCode);
        //            Assert.Equal(_allDataJson, resp.jsonResponse);

        //            var req = server.GetLastRequest();
        //            Assert.Equal("REPORT", req.Method);
        //            Assert.Equal($"/msdk/evalx/user", req.Path);
        //            Assert.Equal("?withReasons=true", req.RawQuery);
        //            Assert.Equal(_mobileKey, req.Headers["Authorization"][0]);
        //            TestUtil.AssertJsonEquals(LdValue.Parse(_userJson), TestUtil.NormalizeJsonUser(LdValue.Parse(req.Body)));
        //        }
        //    });
        //}
    }
}
