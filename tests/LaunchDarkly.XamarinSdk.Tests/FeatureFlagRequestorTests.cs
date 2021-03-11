using System;
using System.Threading.Tasks;
using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Xamarin.Tests
{
    // End-to-end tests of this component against an embedded HTTP server.
    public class FeatureFlagRequestorTests : BaseTest
    {
        private const string _mobileKey = "FAKE_KEY";

        // User key constructed to test base64 encoding of 62 and 63, which differ between the standard and "URL and Filename safe"
        // base64 encodings from RFC4648. We need to use the URL safe encoding for flag requests.
        private static readonly User _user = User.WithKey("foo>bar__?");
        private const string _userJson = "{\"key\":\"foo>bar__?\"}";
        private const string _encodedUser = "eyJrZXkiOiJmb28-YmFyX18_In0=";
        // Note that in a real use case, the user encoding may vary depending on the target platform, because the SDK adds custom
        // user attributes like "os". But the lower-level FeatureFlagRequestor component does not do that.

        private const string _allDataJson = "{}"; // Note that in this implementation, unlike the .NET SDK, FeatureFlagRequestor does not unmarshal the response

        [Fact]
        public async Task GetFlagsUsesCorrectUriAndMethodInGetModeAsync()
        {
            await WithServerAsync(async server =>
            {
                server.ForAllRequests(r => r.WithJsonBody(_allDataJson));

                var config = Configuration.Builder(_mobileKey).BaseUri(new Uri(server.GetUrl()))
                    .Build();

                using (var requestor = new FeatureFlagRequestor(config, _user))
                {
                    var resp = await requestor.FeatureFlagsAsync();
                    Assert.Equal(200, resp.statusCode);
                    Assert.Equal(_allDataJson, resp.jsonResponse);

                    var req = server.GetLastRequest();
                    Assert.Equal("GET", req.Method);
                    Assert.Equal($"/msdk/evalx/users/{_encodedUser}", req.Path);
                    Assert.Equal("", req.RawQuery);
                    Assert.Equal(_mobileKey, req.Headers["Authorization"][0]);
                    Assert.Null(req.Body);
                }
            });
        }

        [Fact]
        public async Task GetFlagsUsesCorrectUriAndMethodInGetModeWithReasonsAsync()
        {
            await WithServerAsync(async server =>
            {
                server.ForAllRequests(r => r.WithJsonBody(_allDataJson));

                var config = Configuration.Builder(_mobileKey).BaseUri(new Uri(server.GetUrl()))
                    .EvaluationReasons(true).Build();

                using (var requestor = new FeatureFlagRequestor(config, _user))
                {
                    var resp = await requestor.FeatureFlagsAsync();
                    Assert.Equal(200, resp.statusCode);
                    Assert.Equal(_allDataJson, resp.jsonResponse);

                    var req = server.GetLastRequest();
                    Assert.Equal("GET", req.Method);
                    Assert.Equal($"/msdk/evalx/users/{_encodedUser}", req.Path);
                    Assert.Equal("?withReasons=true", req.RawQuery);
                    Assert.Equal(_mobileKey, req.Headers["Authorization"][0]);
                    Assert.Null(req.Body);
                }
            });
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
        //            TestUtil.AssertJsonEquals(JToken.Parse(_userJson), TestUtil.NormalizeJsonUser(JToken.Parse(req.Body)));
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
        //            TestUtil.AssertJsonEquals(JToken.Parse(_userJson), TestUtil.NormalizeJsonUser(JToken.Parse(req.Body)));
        //        }
        //    });
        //}
    }
}
