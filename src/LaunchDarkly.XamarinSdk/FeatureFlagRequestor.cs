using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;
using LaunchDarkly.Common;

namespace LaunchDarkly.Xamarin
{
    internal struct WebResponse
    {
        public int statusCode { get; private set; }
        public string jsonResponse { get; private set; }
        public string errorMessage { get; private set; }

        public WebResponse(int code, string response, string error)
        {
            statusCode = code;
            jsonResponse = response;
            errorMessage = error;
        }
    }

    internal interface IFeatureFlagRequestor : IDisposable
    {
        Task<WebResponse> FeatureFlagsAsync();
    }

    internal class FeatureFlagRequestor : IFeatureFlagRequestor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FeatureFlagRequestor));
        private static readonly HttpMethod ReportMethod = new HttpMethod("REPORT");

        private readonly Configuration _configuration;
        private readonly User _currentUser;
        private readonly HttpClient _httpClient;
        private volatile EntityTagHeaderValue _etag;

        internal FeatureFlagRequestor(Configuration configuration, User user)
        {
            this._configuration = configuration;
            this._httpClient = Util.MakeHttpClient(configuration.HttpRequestConfiguration, MobileClientEnvironment.Instance);
            this._currentUser = user;
        }

        public async Task<WebResponse> FeatureFlagsAsync()
        {
            var requestMessage = _configuration.UseReport ? ReportRequestMessage() : GetRequestMessage();
            return await MakeRequest(requestMessage);
        }

        private HttpRequestMessage GetRequestMessage()
        {
            var path = Constants.FLAG_REQUEST_PATH_GET + _currentUser.AsJson().Base64Encode();
            return new HttpRequestMessage(HttpMethod.Get, MakeRequestUriWithPath(path));
        }

        private HttpRequestMessage ReportRequestMessage()
        {
            var request = new HttpRequestMessage(ReportMethod, MakeRequestUriWithPath(Constants.FLAG_REQUEST_PATH_REPORT));
            request.Content = new StringContent(_currentUser.AsJson(), Encoding.UTF8, Constants.APPLICATION_JSON);
            return request;
        }

        private Uri MakeRequestUriWithPath(string path)
        {
            var uri = new UriBuilder(_configuration.BaseUri);
            uri.Path = path;
            if (_configuration.EvaluationReasons)
            {
                uri.Query = "withReasons=true";
            }
            return uri.Uri;
        }

        private async Task<WebResponse> MakeRequest(HttpRequestMessage request)
        {
            using (var cts = new CancellationTokenSource(_configuration.HttpClientTimeout))
            {
                if (_etag != null)
                {
                    request.Headers.IfNoneMatch.Add(_etag);
                }

                try
                {
                    Log.DebugFormat("Getting flags with uri: {0}", request.RequestUri.AbsoluteUri);
                    using (var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            Log.Debug("Get all flags returned 304: not modified");
                            return new WebResponse(304, null, "Get all flags returned 304: not modified");
                        }
                        _etag = response.Headers.ETag;
                        //We ensure the status code after checking for 304, because 304 isn't considered success
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new UnsuccessfulResponseException((int)response.StatusCode);
                        }

                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return new WebResponse(200, content, null);
                    }
                }
                catch (TaskCanceledException tce)
                {
                    if (tce.CancellationToken == cts.Token)
                    {
                        //Indicates the task was cancelled by something other than a request timeout
                        throw;
                    }
                    //Otherwise this was a request timeout.
                    throw new TimeoutException("Get item with URL: " + request.RequestUri +
                                                " timed out after : " + _configuration.HttpClientTimeout);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
